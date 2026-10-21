using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes; // JsonNode, JsonValue
using System.Threading.Tasks;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using org.unirail;
using File = System.IO.File;

/**
 * @class OpenApi_To_AdHoc_Converter
 * @brief Converts OpenAPI (Swagger) specification files (JSON or YAML) into AdHoc protocol definition files.
 *
 * This class processes OpenAPI documents to generate AdHoc protocol definitions. It parses various
 * OpenAPI components like security schemes, servers, responses, parameters, schemas, paths, and
 * operations, translating them into AdHoc protocol constructs (Packs, Fields, Attributes, Actors).
 *
 * Transformation rules
 * ─────────────────────
 * • Path segments              → nested  public interface  containers
 * • HTTP operations            → AdHoc shorthand method signatures  (L____________, …) opId(ReqType req);
 * • Simple schema-ref body     → resolved type used directly          components.schemas.Pet
 * • Query/path param bodies    → top-level  {lastSegment}Req  class
 * • HTTP error codes (4xx/5xx) → top-level  _XXX  sentinel class
 * • "default" response schema  → resolved type used directly
 *
 * @note See https://swagger.io/docs/specification/v3_0/data-models/data-types/ for OpenAPI data types.
 */
class OpenApi_To_AdHoc_Converter{
    static string ProjectName = "";

    // Synthetic packs the converter itself created, grouped by who transmits them.
    // Direction drives the readOnly/writeOnly cleanup: a pack the client sends must not
    // carry readOnly fields; a pack the server sends must not carry writeOnly fields.
    static readonly HashSet<Pack> synthetic_client_sends = [];
    static readonly HashSet<Pack> synthetic_server_sends = [];

    // Dashboard bookkeeping: pack full name → OpenAPI tags of the operations using it,
    // and the set of pack names actually referenced by any operation.
    static readonly Dictionary<string, SortedSet<string>> pack_tags = new();
    static readonly HashSet<string>                       pack_used = [];

    /// <summary>Copies the properties of an OpenAPI object schema into a Pack as fields.</summary>
    static void add_properties(IOpenApiSchema? src, Pack dst)
    {
        if( src?.Properties == null ) return;
        foreach( var (fn, fs) in src.Properties )
        {
            var fld = new Field(dst, fn, fs)
                      {
                          optional = src.Required == null || !src.Required.Contains(fn) ||
                                     (fs.Type.HasValue && fs.Type.Value.HasFlag(JsonSchemaType.Null))
                      };
            fld.add_comment(src.Description);
            if( fs.Deprecated )
                fld.add_attributes(add_attribute("Obsolete", ["Message"], [$"\"{fs.Description ?? "Deprecated"}\""]));
        }
    }

    static string cap(string s) => s.Length == 0 ?
                                       s :
                                       char.IsUpper(s[0]) ?
                                           s[..^1] + s[^1..].ToUpper() : // already capitalized → differ by last char
                                           char.ToUpper(s[0]) + s[1..];

    // ─────────────────────────────────────────────────────────────────────────
    //  Entry point
    // ─────────────────────────────────────────────────────────────────────────
    public static async Task convert(string src_file, string dst_file)
    {
        ProjectName = brush(Path.GetFileNameWithoutExtension(src_file), "");
        using var stream   = File.OpenRead(src_file);
        var       settings = new OpenApiReaderSettings { RuleSet = new ValidationRuleSet() };
        settings.AddYamlReader();

        var readResult = await OpenApiDocument.LoadAsync(stream, format: null, settings: settings);
        var openAPI    = readResult.Document;

        if( readResult.Diagnostic?.Errors.Any() == true )
        {
            Console.Error.WriteLine($"Errors parsing OpenAPI document: {src_file}");
            foreach( var error in readResult.Diagnostic.Errors )
                Console.Error.WriteLine(error.Message);
        }

        // ── Security Schemes ──────────────────────────────────────────────────
        if( 0 < openAPI.Components.SecuritySchemes?.Count )
            foreach( var (name, scheme) in openAPI.Components.SecuritySchemes )
            {
                var schemePack = Pack.get_or_new($"components/security/securitySchemes/{name}");
                schemePack.comment = scheme.Description;

                // Accumulate OAuth2 scopes across all flows into a single set per scheme.
                // Emitted as an enum `{Scheme}Scope { write_pets, read_pets, ... }` with scope
                // descriptions attached as /// comments on each enum field.
                var oauthScopes = new Dictionary<string, string>();
                void AddScopes(IDictionary<string, string> scopes)
                {
                    foreach( var (sn, sd) in scopes )
                        if( !oauthScopes.ContainsKey(sn) )
                            oauthScopes[sn] = sd ?? "";
                }

                switch( scheme.Type )
                {
                    case SecuritySchemeType.OAuth2:
                    {
                        var flows = scheme.Flows;
                        if( flows == null ) break;
                        if( flows.AuthorizationCode != null )
                        {
                            schemePack.attributes += add_attribute("OAuth2AuthAuthorizationCode",
                                                                   ["AuthorizationUrl", "TokenUrl", "RefreshUrl"],
                                                                   [
                                                                       $"\"{flows.AuthorizationCode.AuthorizationUrl}\"",
                                                                       $"\"{flows.AuthorizationCode.TokenUrl}\"",
                                                                       $"\"{flows.AuthorizationCode.RefreshUrl}\""
                                                                   ]);
                            AddScopes(flows.AuthorizationCode.Scopes);
                        }

                        if( flows.Implicit != null )
                        {
                            schemePack.attributes += add_attribute("OAuth2AuthImplicit",
                                                                   ["AuthorizationUrl", "RefreshUrl"],
                                                                   [$"\"{flows.Implicit.AuthorizationUrl}\"", $"\"{flows.Implicit.RefreshUrl}\""]);
                            AddScopes(flows.Implicit.Scopes);
                        }

                        if( flows.Password != null )
                        {
                            schemePack.attributes += add_attribute("OAuth2AuthPassword",
                                                                   ["TokenUrl", "RefreshUrl"],
                                                                   [$"\"{flows.Password.TokenUrl}\"", $"\"{flows.Password.RefreshUrl}\""]);
                            AddScopes(flows.Password.Scopes);
                        }

                        if( flows.ClientCredentials != null )
                        {
                            schemePack.attributes += add_attribute("OAuth2AuthClientCredentials",
                                                                   ["TokenUrl", "RefreshUrl"],
                                                                   [
                                                                       $"\"{flows.ClientCredentials.TokenUrl}\"",
                                                                       $"\"{flows.ClientCredentials.RefreshUrl}\""
                                                                   ]);
                            AddScopes(flows.ClientCredentials.Scopes);
                        }

                        // Emit an enum of scopes if any were collected.
                        if( oauthScopes.Count > 0 )
                        {
                            var scopeEnum = Pack.get_or_new($"components/security/securitySchemes/{name}/{brush(name, "")}Scope");
                            scopeEnum.is_enum = true;
                            foreach( var (sn, sd) in oauthScopes )
                            {
                                var fld = new Field(scopeEnum, sn, (string)null!);
                                if( !string.IsNullOrEmpty(sd) ) fld.comment = sd;
                            }
                        }

                        break;
                    }
                    case SecuritySchemeType.ApiKey:
                        schemePack.attributes += add_attribute("ApiKeyAuth", ["Name", "In"],
                                                               [$"\"{scheme.Name}\"", $"\"{scheme.In}\""]);
                        break;
                    case SecuritySchemeType.Http:
                        if( string.Equals(scheme.Scheme, "bearer", StringComparison.OrdinalIgnoreCase) )
                            schemePack.attributes += add_attribute("BearerAuth", ["BearerFormat"],
                                                                   [$"\"{scheme.BearerFormat}\""]);
                        else if( string.Equals(scheme.Scheme, "basic", StringComparison.OrdinalIgnoreCase) )
                            schemePack.attributes += add_attribute("BasicAuth", [], []);
                        else
                            schemePack.attributes += add_attribute("HttpAuth", ["Scheme"],
                                                                   [$"\"{scheme.Scheme}\""]);
                        break;
                    case SecuritySchemeType.OpenIdConnect:
                        schemePack.attributes += add_attribute("OpenIdConnect", ["OpenIdConnectUrl"],
                                                               [$"\"{scheme.OpenIdConnectUrl}\""]);
                        break;
                    case (SecuritySchemeType)5:
                        schemePack.attributes += add_attribute("MutualTLS", [], []);
                        break;
                }

                if( scheme.Extensions != null )
                    foreach( var (ek, ev) in scheme.Extensions )
                        schemePack.attributes +=
                            add_attribute("SecurityExtension", ["Key", "Value"], [$"\"{ek}\"", $"\"{ev}\""], true);
            }


        if( 0 < openAPI.Security?.Count )
        {
            // Attach as attributes on the root project interface via a synthetic pack
            var secPack = Pack.get_or_new("security/global");
            foreach( var requirement in openAPI.Security )
                foreach( var (schemeRef, scopes) in requirement )
                    secPack.attributes += add_attribute("GlobalSecurity",
                                                        ["Scheme", "Scopes"],
                                                        [
                                                            $"\"{schemeRef.Name ?? schemeRef.Reference?.Id}\"",
                                                            $"\"{string.Join(",", scopes)}\""
                                                        ], true);
        }

        // ── Servers → Hosts ───────────────────────────────────────────────────
        read_servers(openAPI.Servers);

        // ── Components / Responses ────────────────────────────────────────────
        if( 0 < openAPI.Components?.Responses?.Count )
            create_response_packs("components/responses", openAPI.Components.Responses, null);

        if( 0 < openAPI.Components?.Headers?.Count )
        {
            var components_headers = Pack.get_or_new("components/headers");
            foreach( var (name, header) in openAPI.Components.Headers )
            {
                var fld = new Field(components_headers, name, header.Schema)
                          { optional = !header.Required };
                fld.add_comment(header.Description);
                if( header.Deprecated )
                    fld.add_attributes(add_attribute("Obsolete", ["Message"], [$"\"{(header.Description ?? "Deprecated")}\""]));
            }
        }

        // ── Components / Schemas ──────────────────────────────────────────────
        if( 0 < openAPI.Components?.Schemas?.Count )
        {
            var components_schemas = Pack.get_or_new("components/schemas");

            foreach( var (name, schema) in openAPI.Components!.Schemas! )
            {
                if( schema is OpenApiSchemaReference shw &&
                    GetReferencePath(shw.Reference) != "#/components/schemas/" + name )
                {
                    var p = Pack.get_or_new("components/schemas/" + name);
                    p.add_comment(schema.Description);
                    p.Reference = GetReferencePath(shw.Reference);
                    continue;
                }

                if( schema.AdditionalProperties != null )
                {
                    new Field(components_schemas, name, schema.AdditionalProperties, true)
                        .add_comment(schema.Description);
                    continue;
                }


                if( schema.Type != null                                         &&
                    (schema.Properties == null || schema.Properties.Count == 0) &&
                    (schema.AllOf      == null || schema.AllOf.Count      == 0) &&
                    (schema.OneOf      == null || schema.OneOf.Count      == 0) )
                {
                    new Field(components_schemas, name, schema).add_comment(schema.Description);
                    continue;
                }


                var pack = Pack.get_or_new("components/schemas/" + name);

                var docBuilder = new StringBuilder();
                if( !string.IsNullOrEmpty(schema.Title) ) docBuilder.AppendLine($"TITLE: {schema.Title}");
                if( !string.IsNullOrEmpty(schema.Description) ) docBuilder.AppendLine(schema.Description);
                if( schema.ExternalDocs != null )
                {
                    docBuilder.AppendLine($"External Docs: {schema.ExternalDocs.Description} {schema.ExternalDocs.Url}");
                    pack.attributes += add_attribute("ExternalDocs", ["Description", "Url"], [$"\"{schema.ExternalDocs.Description}\"", $"\"{schema.ExternalDocs.Url}\""]);
                }

                pack.comment = docBuilder.ToString();

                if( schema.Deprecated )
                {
                    var msg = string.IsNullOrEmpty(schema.Description) ?
                                  "Deprecated" :
                                  schema.Description;
                    pack.attributes += add_attribute("Obsolete", ["Message"], [$"\"{msg}\""]);
                }

                // Examples — AllowMultiple. `examples` (map) wins over single `example` when both present.
                if( schema.Examples != null && 0 < schema.Examples.Count )
                    foreach( var ex in schema.Examples )
                        pack.attributes += add_attribute("Example", ["Value"],
                                                         [$"\"{ex}\""], true);
                else if( schema.Example != null )
                    pack.attributes += add_attribute("Example", ["Value"],
                                                     [$"\"{schema.Example}\""], true);

                add_properties((OpenApiSchema)schema, pack);


                if( schema.AllOf != null )
                    foreach( var ao in schema.AllOf )
                        if( ao is OpenApiSchemaReference sh ) pack.add_inherits(GetReferencePath(sh.Reference));
                        else add_properties(ao, pack);

                var inline = 0;

                void process_polymorphism(IList<IOpenApiSchema> schemas, string prefix)
                {
                    if( schemas == null ) return;
                    foreach( var ps in schemas )
                        if( ps is OpenApiSchemaReference sh )
                            new Field(pack, $"{prefix}_{sh.Reference.Id}", ps) { optional = true };
                        else
                        {
                            var dst = Pack.get_or_new($"components/schemas/{name}/{prefix}{inline++}");
                            add_properties(ps, dst);
                            new Field(pack, $"{prefix}_{dst.name}", dst.name) { optional = true };
                        }
                }

                process_polymorphism(schema.OneOf, "OneOf");
                process_polymorphism(schema.AnyOf, "AnyOf");

                if( schema.Discriminator != null )
                {
                    // Keep the propertyName (identifies which field carries the type tag)
                    pack.attributes += add_attribute("DiscriminatorProperty", ["Property"],
                                                     [$"\"{schema.Discriminator.PropertyName}\""]);

                    // Wire each mapping entry as real inheritance: Sub : Base.
                    // Also tag Sub with its discriminator value via [DiscriminatorValue("dog")].
                    // Use get_or_new so this still works when the target schema hasn't been
                    // iterated yet (OpenAPI enumeration order is not guaranteed).
                    if( schema.Discriminator.Mapping != null )
                    {
                        var basePath = $"#/components/schemas/{name}";
                        foreach( var (discValue, mappingTarget) in schema.Discriminator.Mapping )
                        {
                            var targetRef = mappingTarget?.Reference != null ?
                                                GetReferencePath(mappingTarget.Reference) :
                                                null;
                            if( targetRef == null ) continue;
                            var subPack = Pack.get_or_new(targetRef);
                            subPack.add_inherits(basePath);
                            subPack.attributes += add_attribute("DiscriminatorValue", ["Value"],
                                                                [$"\"{discValue}\""]);
                        }
                    }
                }

                if( schema.Not != null )
                    pack.add_comment($"CONSTRAINT: Must NOT validate against: {schema.Not.Description ?? "anonymous sub-schema"}");

                if( schema.Extensions != null )
                    foreach( var (ek, ev) in schema.Extensions )
                        pack.attributes += add_attribute("Extension",                ["ExtKey", "ExtValue"],
                                                         [$"\"{ek}\"", $"\"{ev}\""], true);
            }
        }

        // ── Components / Parameters ───────────────────────────────────────────
        // `in:` (path/query/header/cookie), style, explode, allowReserved are URL-serialization
        // artifacts — dropped. Each reusable parameter becomes a plain field.
        if( 0 < openAPI.Components?.Parameters?.Count )
        {
            var components_parameters = Pack.get_or_new("components/parameters");

            foreach( var (name, parameter) in openAPI.Components.Parameters )
            {
                var targetSchema = parameter.Schema ?? parameter.Content.Values.FirstOrDefault()?.Schema;

                var fld = new Field(components_parameters, name, targetSchema) { optional = !parameter.Required };
                fld.add_comment(parameter.Description);
                if( parameter.Deprecated )
                    fld.add_attributes(add_attribute("Obsolete", ["Message"], [$"\"{(parameter.Description ?? "Deprecated")}\""]));
                if( parameter.Example != null ) fld.add_comment($"example: {parameter.Example}");
                if( parameter.Extensions?.Count > 0 )
                    foreach( var (ek, ev) in parameter.Extensions )
                        fld.add_attributes(add_attribute("Extension", ["Key", "Value"],
                                                         [$"\"{ek}\"", $"\"{ev}\""], true));
            }
        }

        // Request bodies: AdHoc is a single binary format — media types are HTTP-era noise.
        // Pick one schema (preferring application/json) and collapse.
        if( 0 < openAPI.Components?.RequestBodies?.Count )
            foreach( var (name, requestBody) in openAPI.Components.RequestBodies )
            {
                var rbPack = Pack.get_or_new($"components/requestBodies/{name}");
                rbPack.add_comment(requestBody.Description);
                if( requestBody.Required )
                    rbPack.attributes += add_attribute("Required", [], []);

                var picked = pick_schema(requestBody.Content);
                if( picked is OpenApiSchemaReference schRef )
                    rbPack.add_inherits(GetReferencePath(schRef.Reference));
                else if( picked != null )
                    add_properties(picked, rbPack);
            }

        // ── Paths / Operations ────────────────────────────────────────────────
        // AdHoc has no URL. Path/query/header parameters are all just fields of the request pack.
        // Media-type content negotiation is HTTP baggage; we pick one representative schema.
        // ──────────────────────────────────────────────────────────────────────
        foreach( var (path, item) in openAPI.Paths )
        {
            // Plain field emission — no [In]/[Style]/[Explode]/[AllowReserved]. Pure data.
            void addParamField(IOpenApiParameter src, Pack dst)
            {
                var fld = new Field(dst, src.Name!, src.Schema) { optional = !src.Required };
                fld.add_comment(src.Description);
                if( src.Deprecated )
                    fld.add_attributes(add_attribute("Obsolete", ["Message"], [$"\"{(src.Description ?? "Deprecated")}\""]));
            }

            foreach( var (httpMethod, operation) in item.Operations )
            {
                var opUniqueName = string.IsNullOrEmpty(operation.OperationId) ?
                                       brush(httpMethod + "_" + clean_path(path), "") :
                                       brush(operation.OperationId,               "");

                var actor = Actor.get_or_new($"paths/{clean_path(path)}/{opUniqueName}");

                if( !string.IsNullOrEmpty(operation.Summary) ) actor.add_comment(operation.Summary);
                if( !string.IsNullOrEmpty(operation.Description) && operation.Description != operation.Summary )
                    actor.add_comment(operation.Description);

                // The original HTTP route — becomes constants in the generated code, so an HTTP
                // gateway/facade for legacy clients (or reverse mapping) can be built on top.
                actor.attributes += add_attribute("HttpRoute", ["Method", "Path"],
                                                  [$"\"{httpMethod.ToString().ToUpperInvariant()}\"", $"\"{path}\""]);

                // Tags: structured attribute AND appended to doc pool (for KeepDoc downstream filters)
                if( 0 < operation.Tags?.Count )
                {
                    var tagNames = string.Join(", ", operation.Tags.Select(t => t.Name));
                    actor.add_comment($"tags: {tagNames}");
                    actor.tags.AddRange(operation.Tags.Where(t => !string.IsNullOrEmpty(t.Name)).Select(t => t.Name!));
                    foreach( var tag in operation.Tags )
                    {
                        actor.attributes += add_attribute("Tag", ["Name", "Description"], [$"\"{tag.Name}\"", $"\"{tag.Description}\""], true);
                        if( tag.ExternalDocs != null )
                            actor.attributes += add_attribute("TagExternalDocs",
                                                              ["TagName", "Description", "Url"],
                                                              [$"\"{tag.Name}\"", $"\"{tag.ExternalDocs.Description}\"", $"\"{tag.ExternalDocs.Url}\""], true);
                    }
                }

                // Deprecation with description-derived message
                if( operation.Deprecated )
                {
                    var msg = string.IsNullOrEmpty(operation.Description) ?
                                  (operation.Summary ?? "Deprecated") :
                                  operation.Description;
                    actor.attributes += add_attribute("Obsolete", ["Message"], [$"\"{msg}\""]);
                }

                if( operation.ExternalDocs != null )
                    actor.attributes += add_attribute("ExternalDocs", ["Description", "Url"],
                                                      [$"\"{operation.ExternalDocs.Description}\"", $"\"{operation.ExternalDocs.Url}\""]);

                // Security requirements — structured + doc pool
                if( 0 < operation.Security?.Count )
                {
                    var schemeNames = new HashSet<string>();
                    foreach( var requirement in operation.Security )
                        foreach( var (schemeRef, scopes) in requirement )
                        {
                            var schemeName = schemeRef.Reference?.Id ?? schemeRef.Name ?? "UnknownScheme";
                            schemeNames.Add(schemeName);
                            actor.attributes += add_attribute("SecurityRequirement",
                                                              ["Ref", "Scheme", "Scopes"],
                                                              [$"\"{schemeRef}\"", $"\"{schemeName}\"", $"\"{string.Join(",", scopes)}\""], true);
                        }
                    if( schemeNames.Count > 0 )
                        actor.add_comment($"security: {string.Join(", ", schemeNames)}");
                }

                // Callbacks — server pushes to client on an existing connection.
                // The URL expression key (e.g. `{$request.body#/callbackUrl}`) is HTTP-routing noise;
                // in AdHoc the connection already exists, so the key is dropped.
                if( operation.Callbacks != null && operation.Callbacks.Count > 0 )
                    foreach( var (cbName, callback) in operation.Callbacks )
                        foreach( var (cbPath, cbItem) in callback.PathItems )
                            foreach( var (cbOpType, cbOp) in cbItem.Operations )
                            {
                                // Server → Client push
                                var cbReq = pick_schema(cbOp.RequestBody?.Content);
                                if( cbReq is OpenApiSchemaReference schRef )
                                    actor.callbackRPacks.Add(new Actor.Param(get(GetReferencePath(schRef.Reference)) as Pack));
                                else if( cbReq != null )
                                {
                                    var synth = Pack.get_or_new($"requests/{clean_path(path)}/{opUniqueName}/cb/{cbName}_Req");
                                    add_properties(cbReq, synth);
                                    synthetic_server_sends.Add(synth); // callback push travels server → client
                                    actor.callbackRPacks.Add(new Actor.Param(synth));
                                }

                                // Client → Server ack
                                if( cbOp.Responses != null )
                                    foreach( var (code, resp) in cbOp.Responses )
                                    {
                                        var respSchema = pick_schema(resp.Content);
                                        if( respSchema is OpenApiSchemaReference rsRef )
                                            actor.callbackLPacks.Add(new Actor.Param(get(GetReferencePath(rsRef.Reference)) as Pack));
                                        else if( respSchema != null )
                                        {
                                            var synth = Pack.get_or_new($"responses/{clean_path(path)}/{opUniqueName}/cb/{cbName}_Resp_{code}");
                                            add_properties(respSchema, synth);
                                            synthetic_client_sends.Add(synth); // callback ack travels client → server
                                            actor.callbackLPacks.Add(new Actor.Param(synth));
                                        }
                                        else if( code.Length > 0 && char.IsDigit(code[0]) )
                                            actor.callbackLPacks.Add(new Actor.Param(Pack.get_or_new($"Code_{code}")));
                                    }
                            }

                // ── Decide request type ─────────────────────────────────────
                // Merge path-level + operation-level parameters (op overrides path on (name, in))
                var mergedParams = new List<IOpenApiParameter>(item.Parameters ?? []);
                if( operation.Parameters != null )
                    foreach( var opP in operation.Parameters )
                    {
                        mergedParams.RemoveAll(p => p.Name == opP.Name && p.In == opP.In);
                        mergedParams.Add(opP);
                    }

                // Shortcut: direct $ref requestBody with no extra params → use type directly
                if( operation.RequestBody is OpenApiRequestBodyReference rbRef )
                {
                    var resolved = get(GetReferencePath(rbRef.Reference)) as Pack;
                    if( resolved != null && mergedParams.Count == 0 )
                    {
                        actor.request = new Actor.Param(resolved);
                        goto skip_body;
                    }
                }

                var bodySchema = pick_schema(operation.RequestBody?.Content);

                // Shortcut: direct $ref body schema with no extra params → use type directly
                if( mergedParams.Count == 0 && bodySchema is OpenApiSchemaReference bodyRef )
                {
                    var resolvedPack = get(GetReferencePath(bodyRef.Reference)) as Pack;
                    if( resolvedPack != null )
                    {
                        actor.request = new Actor.Param(resolvedPack);
                        goto skip_body;
                    }
                }

                // No body, no params → NoArg
                if( operation.RequestBody == null && mergedParams.Count == 0 )
                {
                    actor.request = null;
                    goto skip_body;
                }

                // Otherwise build a synthetic {opId}Req pack flattening body + params
                var reqPack = Pack.get_or_new(opUniqueName + "Req");
                synthetic_client_sends.Add(reqPack); // requests travel client → server

                // Body schema → inherit ($ref) / File conduit (raw bytes) / inline fields / single `body` field
                if( bodySchema is OpenApiSchemaReference bRef )
                    reqPack.add_inherits(GetReferencePath(bRef.Reference));
                else if( is_binary_schema(bodySchema) || (bodySchema == null && 0 < operation.RequestBody?.Content?.Count) )
                    add_file_conduit(reqPack, "body", operation.RequestBody?.Description); // upload → streamed, not buffered
                else if( bodySchema != null )
                    if( 0 < bodySchema.Properties?.Count )
                        add_properties(bodySchema, reqPack);
                    else
                        new Field(reqPack, "body", bodySchema); // primitive / array / map body — keep it, don't drop

                // All parameters (path/query/header/cookie) → flat fields
                foreach( var parameter in mergedParams )
                    addParamField(parameter, reqPack);

                actor.request = new Actor.Param(reqPack);

                skip_body: ;

                // Responses
                if( 0 < operation.Responses?.Count )
                    create_response_packs($"responses3/{clean_path(path)}/{opUniqueName}", operation.Responses, actor);
            }
        }

        // ── Webhooks (OpenAPI 3.1) — server-initiated pushes on a separate connection.
        // Emitted as actors under `root_webhook_actor`, which is wired to
        // `Connects<Server0, Subscriber>` in the output template.
        if( openAPI.Webhooks != null && openAPI.Webhooks.Count > 0 )
            foreach( var (whName, whItem) in openAPI.Webhooks )
                foreach( var (httpMethod, operation) in whItem.Operations )
                {
                    var opName = string.IsNullOrEmpty(operation.OperationId) ?
                                     brush(whName + "_" + httpMethod, "") :
                                     brush(operation.OperationId, "");

                    var actor = Actor.get_or_new($"webhooks/{whName}/{opName}", root_webhook_actor);
                    if( !string.IsNullOrEmpty(operation.Summary) ) actor.add_comment(operation.Summary);
                    if( !string.IsNullOrEmpty(operation.Description) && operation.Description != operation.Summary )
                        actor.add_comment(operation.Description);

                    actor.attributes += add_attribute("HttpRoute", ["Method", "Path"],
                                                      [$"\"{httpMethod.ToString().ToUpperInvariant()}\"", $"\"{whName}\""]);

                    actor.tags.Add("webhook");
                    if( 0 < operation.Tags?.Count )
                        actor.tags.AddRange(operation.Tags.Where(t => !string.IsNullOrEmpty(t.Name)).Select(t => t.Name!));

                    // Webhook "request" is the server's push payload.
                    var bodySchema = pick_schema(operation.RequestBody?.Content);
                    if( bodySchema is OpenApiSchemaReference bRef )
                    {
                        var resolved = get(GetReferencePath(bRef.Reference)) as Pack;
                        if( resolved != null ) actor.request = new Actor.Param(resolved);
                    }
                    else if( bodySchema != null )
                    {
                        var synth = Pack.get_or_new($"webhooks/{whName}/{opName}Req");
                        add_properties(bodySchema, synth);
                        synthetic_server_sends.Add(synth); // the push payload travels server → subscriber
                        actor.request = new Actor.Param(synth);
                    }

                    // Subscriber's ack responses (they travel subscriber → server)
                    if( 0 < operation.Responses?.Count )
                        create_response_packs($"webhookResponses/{whName}/{opName}", operation.Responses, actor, server_sends: false);
                }

        // Now that every actor has been registered, resolve all deferred links.
        // The qualified prefix mirrors the generated C# namespace + interface nesting.
        ResolveAllLinks(root_actor, $"com.my.company.{ProjectName}.ClientServerConnection");
        ResolveAllLinks(root_webhook_actor, $"com.my.company.{ProjectName}.ServerSubscriberConnection");

        // readOnly/writeOnly → direction projections (_Read/_Write) + cleanup of synthetic packs.
        apply_direction_semantics();

        // Dashboard bookkeeping (must run before the StandardErrors substitution below, so the
        // individual error packs keep their tags).
        collect_dashboard_info(root_actor);
        collect_dashboard_info(root_webhook_actor);

        // ── Error pack de-duplication: collect error types that recur in ≥3 actors,
        //    emit a shared `StandardErrors` Pack Set, and substitute individual params
        //    with the set name in each affected actor. ───────────────────────────
        var errorTypeUsage = new Dictionary<string, int>();
        void tallyErrors(Actor n)
        {
            if( n != root_actor )
                foreach( var p in n.response )
                {
                    if( string.IsNullOrEmpty(p.httpCode) ) continue;
                    var isError = p.httpCode.Equals("default", StringComparison.OrdinalIgnoreCase) ||
                                  (char.IsDigit(p.httpCode[0]) && int.TryParse(p.httpCode, out var c) && c >= 400);
                    if( !isError ) continue;
                    var tn = p.GetTypeName();
                    if( string.IsNullOrEmpty(tn) || tn == "NoArg" ) continue;
                    errorTypeUsage[tn] = errorTypeUsage.GetValueOrDefault(tn) + 1;
                }

            foreach( var c in n.children ) tallyErrors(c);
        }

        tallyErrors(root_actor);

        var sharedErrorTypes = errorTypeUsage
                              .Where(kv => kv.Value >= 3)
                              .Select(kv => kv.Key)
                              .OrderBy(s => s)
                              .ToList();

        // Build an emitted StandardErrors interface block (or empty if no shared errors)
        var standardErrorsDecl = "";
        if( sharedErrorTypes.Count > 0 )
        {
            // C# tuples need ≥2 elements — a single shared type is passed to _<> directly.
            var setExpr = sharedErrorTypes.Count == 1 ?
                              sharedErrorTypes[0] :
                              $"({string.Join(", ", sharedErrorTypes)})";
            standardErrorsDecl = $"    /// <summary>Shared error pack set — error schemas reused across operations.</summary>\n" +
                                 $"    public interface StandardErrors : _<{setExpr}> {{ }}\n";

            // Substitute: when an actor's response list contains a shared-error param, rewrite it
            // to reference `StandardErrors` instead, and dedupe.
            var sharedSet = new HashSet<string>(sharedErrorTypes);
            void substitute(Actor n)
            {
                if( n != root_actor )
                {
                    var seenStandardErrors = false;
                    var newResponses       = new List<Actor.Param>();
                    foreach( var p in n.response )
                        if( sharedSet.Contains(p.GetTypeName()) )
                        {
                            if( seenStandardErrors ) continue;
                            seenStandardErrors = true;
                            newResponses.Add(new Actor.Param("StandardErrors") { comment = "shared errors", httpCode = p.httpCode });
                        }
                        else { newResponses.Add(p); }

                    if( seenStandardErrors )
                    {
                        n.response.Clear();
                        n.response.AddRange(newResponses);
                    }
                }

                foreach( var c in n.children ) substitute(c);
            }

            substitute(root_actor);
        }

        // Force every field's type to resolve once — this lazily instantiates format TYPEDEF
        // alias Packs (Uuid, IPv4, IPv6, …) and inline-object packs before the write pass
        // starts. Without this, any pack created mid-write lands in a sibling that's already
        // been enumerated and silently gets dropped from the emitted output. Resolution can
        // itself create packs with unresolved fields → iterate to a fixpoint.
        void pre_resolve(Pack p)
        {
            foreach( var fld in p.fields.ToList() ) fld.get_type_string();
            foreach( var c in p.children.ToList() ) pre_resolve(c);
        }

        int count_packs(Pack p) => 1 + p.children.Sum(count_packs);

        for( int before = -1, after = count_packs(root); before != after; before = after, after = count_packs(root) )
            pre_resolve(root);

        // The Dashboard block — after pre_resolve, so lazily created packs are listed too.
        var dashboard = build_dashboard();

        // ── Write output ──────────────────────────────────────────────────────
        var sb = new StringBuilder();
        root.write(sb);

        var sb2 = new StringBuilder();
        root_actor.write(sb2);

        var sb3 = new StringBuilder();
        root_webhook_actor.write(sb3);
        var hasWebhooks = sb3.Length > 0;

        var tagAttributes = new StringBuilder();
        if( openAPI.Tags != null && openAPI.Tags.Count > 0 )
            foreach( var tag in openAPI.Tags )
            {
                // 1. Basic Tag Info
                tagAttributes.AppendLine(add_attribute("TagDefinition",
                                                       ["Name", "Description"],
                                                       [$"\"{tag.Name}\"", $"\"{tag.Description}\""], true));

                // 2. Tag-specific External Docs
                if( tag.ExternalDocs != null )
                    tagAttributes.AppendLine(add_attribute("TagExternalDocs",
                                                           ["TagName", "Description", "Url"],
                                                           [$"\"{tag.Name}\"", $"\"{tag.ExternalDocs.Description}\"", $"\"{tag.ExternalDocs.Url}\""], true));
            }

        File.WriteAllText(dst_file, $@"
using System;
using org.unirail.Meta;

namespace com.my.company // Your company namespace. Required!
{{
    /**
        Packs Inventory. One line per transmittable pack, pre-tagged with the OpenAPI tags
        of the operations using it — route packs in branches by these tags (KeepDoc). Add
        your own tags/emojis after the `/>`. On its first run AdHocAgent carries the tags
        into its numbers tables, numbers the packs and gives a wire id to each pack that
        becomes directly transmittable.

{dashboard}
    */
    {(string.IsNullOrEmpty(openAPI.Info?.Title) ? "" : add_attribute("Title", ["Title"], [$"\"{openAPI.Info.Title}\""]))}
    {(string.IsNullOrEmpty(openAPI.Info?.Version) ? "" : add_attribute("Version", ["Version"], [$"\"{openAPI.Info.Version}\""]))}
    {(string.IsNullOrEmpty(openAPI.Info?.Description) ? "" : add_attribute("Description", ["Description"], [$"\"{openAPI.Info.Description}\""]))}
    {(openAPI.Info?.Contact == null ? "" : add_attribute("Contact", ["Name", "Email", "Url"], [$"\"{openAPI.Info.Contact?.Name}\"", $"\"{openAPI.Info.Contact?.Email}\"", $"\"{openAPI.Info.Contact?.Url}\""]))}
    {(string.IsNullOrEmpty(openAPI.Info?.License?.Name) ? "" : add_attribute("License", ["Name", "Identifier", "Url"], [$"\"{openAPI.Info.License?.Name ?? "N/A"}\"", $"\"{openAPI.Info.License?.Identifier ?? ""}\"", $"\"{openAPI.Info.License?.Url}\""]))}
    {(openAPI.ExternalDocs == null ? "" : add_attribute("ExternalDocs", ["Description", "Url"], [$"\"{openAPI.ExternalDocs.Description}\"", $"\"{openAPI.ExternalDocs.Url}\""]))}
    {(string.IsNullOrEmpty(openAPI.Info?.TermsOfService?.ToString()) ? "" : add_attribute("TermsOfService", ["Url"], [$"\"{openAPI.Info.TermsOfService}\""]))}
    public interface {ProjectName}
    {{
    {tagAttributes}
    {standardErrorsDecl}
    {sb}
    {hosts}
    ///<see cref = 'InTS'/>   implementation in TypeScript
    ///<see cref = 'InCS'/>   implementation in C#
    ///<see cref = 'InJAVA'/> implementation in JAVA
    ///<see cref = 'InCPP'/>  implementation in C++
    ///<see cref = 'InRS'/>   implementation in RUST
    ///<see cref = 'InGO'/>   implementation in GO
    struct Client : Host{{}}

    /// <summary>
    /// Synthetic pack representing an empty request or response.
    /// Used when an operation has no parameters or body.
    /// </summary>
    public class NoArg {{}}

    /// <summary>
    /// Global caps for collections that carry no explicit [D(...)] attribute. AdHoc defaults
    /// everything to 255 items/chars. JSON APIs rarely declare bounds — review these and
    /// raise them to match your real payloads (an oversized incoming value is a protocol error).
    /// </summary>
    enum _DefaultMaxLengthOf
    {{
        Arrays  = 255,
        Maps    = 255,
        Sets    = 255,
        Strings = 255,
    }}

    interface ClientServerConnection : Connects<Client, Server>{{
        {sb2}
    }}

    {(hasWebhooks ? @$"
    ///<see cref = 'InTS'/>   implementation in TypeScript
    ///<see cref = 'InCS'/>   implementation in C#
    ///<see cref = 'InJAVA'/> implementation in JAVA
    ///<see cref = 'InCPP'/>  implementation in C++
    ///<see cref = 'InRS'/>   implementation in RUST
    ///<see cref = 'InGO'/>   implementation in GO
    struct Subscriber : Host {{}}

    interface ServerSubscriberConnection : Connects<Server, Subscriber>{{
        {sb3}
    }}
" : "")}

    {string.Join('\n', attributes.Values)}

    }}
}}
");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  create_response_packs
    // ─────────────────────────────────────────────────────────────────────────
    /**
     * @brief Creates AdHoc response descriptors for OpenAPI Responses.
     *
     * For reusable component responses (actor == null), creates Pack entries in the Pack hierarchy.
     * For operation-specific responses (actor != null), applies the following rules:
     *
     *   • 2xx with a direct schema $ref  → Actor.Param with the resolved type name (e.g. "components.schemas.Pet")
     *   • HTTP status code (digit-led)   → Actor.Param backed by a top-level  _XXX  sentinel class
     *   • "default" with schema $ref     → Actor.Param with the resolved type name
     *   • everything else                → Actor.Param backed by a generated Pack
     *
     * Multiple 2xx responses that share the same schema $ref are deduplicated to one Param.
     */
    static void create_response_packs(string path, IDictionary<string, IOpenApiResponse> Responses, Actor? actor, bool server_sends = true)
    {
        var direction_set = server_sends ?
                                synthetic_server_sends :
                                synthetic_client_sends;

        if( actor == null )
        {
            // ── Reusable component responses ──────────────────────────────
            foreach( var (name, response) in Responses )
            {
                var packName = char.IsDigit(name[0]) ?
                                   $"Code_{name}" :
                                   name;
                var p = Pack.get_or_new($"{path}/{packName}");
                p.add_comment(response.Description);

                var schema = pick_schema(response.Content);
                if( schema is OpenApiSchemaReference schRef )
                    p.add_inherits(GetReferencePath(schRef.Reference));
                else if( is_binary_schema(schema) )
                    add_file_conduit(p, "value", response.Description); // download → streamed, not buffered
                else if( schema != null )
                    if( 0 < schema.Properties?.Count )
                        add_properties(schema, p); // inline object → real fields, not a Binary blob
                    else
                        new Field(p, "value", schema);
            }

            return;
        }

        // ── Operation-specific responses ──────────────────────────────────
        var emittedTypeNames = new HashSet<string>();

        foreach( var (code, response) in Responses )
        {
            if( code == "default" && (response.Content == null || response.Content.Count == 0) ) continue;

            Actor.Param param;
            Pack?       opSpecificPack = null; // a pack this op owns — eligible for a HeaderFor overlay

            // ── Try to resolve a direct schema $ref ───────────────────────
            string? resolvedTypeName = null;
            var     respSchema       = pick_schema(response.Content);
            if( respSchema is OpenApiSchemaReference schRef )
            {
                var resolved = get(GetReferencePath(schRef.Reference)) as Pack;
                resolvedTypeName = resolved?.ToString();
            }
            else if( respSchema != null )
            {
                // Array, Map, raw bytes, or inline object — wrap in a synthetic response pack
                var wrapperPack = Pack.get_or_new($"{path}/Response_{code}");
                wrapperPack.add_comment(response.Description);
                if( is_binary_schema(respSchema) )
                    add_file_conduit(wrapperPack, "value", null); // download → streamed, not buffered
                else if( 0 < respSchema.Properties?.Count )
                    add_properties(respSchema, wrapperPack); // inline object → real fields, not a Binary blob
                else
                    new Field(wrapperPack, "value", respSchema);
                direction_set.Add(wrapperPack);
                opSpecificPack   = wrapperPack;
                resolvedTypeName = wrapperPack.ToString();
            }

            if( resolvedTypeName != null )
            {
                if( !emittedTypeNames.Add(resolvedTypeName) ) goto process_links;
                param = new Actor.Param(resolvedTypeName) { comment = response.Description ?? "", httpCode = code }; // ← httpCode
            }
            else if( code.Length > 0 && char.IsDigit(code[0]) )
            {
                var sentinelPack = Pack.get_or_new($"Code_{code}"); // shared, project-wide sentinel
                param = new Actor.Param(sentinelPack) { comment = response.Description ?? "", httpCode = code }; // ← httpCode
            }
            else
            {
                var fallbackPack = Pack.get_or_new($"{path}/Code_{code}");
                fallbackPack.add_comment(response.Description);
                opSpecificPack = fallbackPack;
                param          = new Actor.Param(fallbackPack) { comment = response.Description ?? "", httpCode = code }; // ← httpCode
            }

            actor.response.Add(param);

            // ── HTTP response headers ──────────────────────────────────────
            // AdHoc packet headers accept only single, primitive, non-nullable fields, and they
            // attach per PACK. So: numeric/bool headers become a HeaderFor<> overlay — but only
            // when the response pack is op-specific (a shared $ref/sentinel pack would get the
            // header globally). Everything else is preserved as documentation on the operation.
            if( 0 < response.Headers?.Count )
            {
                Pack? headerPack = null;
                foreach( var (hName, header) in response.Headers )
                {
                    var hSchema = header.Schema;
                    var hType   = hSchema?.Type == null ? null :
                                  hSchema.Type.Value.HasFlag(JsonSchemaType.Integer) ? hSchema.Format == "int64" ? "long" : "int" :
                                  hSchema.Type.Value.HasFlag(JsonSchemaType.Number) ? hSchema.Format  == "float" ? "float" : "double" :
                                  hSchema.Type.Value.HasFlag(JsonSchemaType.Boolean) ? "bool" :
                                                                                       null;

                    if( hType != null && opSpecificPack != null )
                    {
                        if( headerPack == null )
                        {
                            headerPack = Pack.get_or_new($"{path}/Headers_{code}");
                            headerPack.HeaderFor = opSpecificPack.ToString();
                            headerPack.add_comment($"HTTP response headers of {code}, parsed before the payload.");
                        }

                        // inline_type field: no schema → no [MinMax]/[A]/[V] attributes, which
                        // are forbidden on header fields (fixed wire size required).
                        new Field(headerPack, hName, hType).add_comment(header.Description);
                    }
                    else // string-typed or attached to a shared pack — keep as documentation
                        actor.add_comment($"HTTP header on {code}: {hName}{(hSchema?.Type == null ? "" : $" ({hSchema.Type})")}{(string.IsNullOrEmpty(header.Description) ? "" : $" — {header.Description}")}");
                }
            }

            process_links:
            // ── Defer link resolution: actors may not exist yet ──────────────────
            if( response.Links != null && response.Links.Count > 0 )
            {
                if( !actor.pendingLinkOpIds.ContainsKey(code) )
                    actor.pendingLinkOpIds[code] = new List<string>();

                foreach( var (_, link) in response.Links )
                {
                    if( !string.IsNullOrEmpty(link.OperationId) )
                        actor.pendingLinkOpIds[code].Add(link.OperationId);
                    else if( !string.IsNullOrEmpty(link.OperationRef) )
                        actor.pendingLinkOpIds[code].Add("$ref:" + link.OperationRef);
                }
            }
        }
    }
    // ─────────────────────────────────────────────────────────────────────────
    //  readOnly / writeOnly direction semantics (SSOT projections)
    //
    //  OpenAPI: readOnly fields exist only in server→client data, writeOnly only in
    //  client→server data. Mapping to AdHoc:
    //    • synthetic packs the converter owns → offending fields are simply removed;
    //    • shared schema packs → a `{Name}_Write` / `{Name}_Read` projection is generated
    //      (C# inheritance + `<see cref="Pack.field"/>-` exclusion — the AdHoc mixin model,
    //      keeping the source pack as the Single Source of Truth), and every actor param
    //      is re-pointed to the projection matching its direction.
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Finds a Pack in the tree by its dotted full name; null when not a tree pack.</summary>
    static Pack? find_pack(string dotted)
    {
        var p = root;
        foreach( var n in dotted.Split('.') )
        {
            p = p.children.FirstOrDefault(c => c.name == n)!;
            if( p == null ) return null;
        }

        return p == root ?
                   null :
                   p;
    }

    /// <summary>Collects the pack's readOnly (or writeOnly) fields, including inherited ones, with their declaring pack.</summary>
    static void collect_direction_fields(Pack p, bool read_only, List<(Pack owner, Field f)> acc, HashSet<Pack> seen)
    {
        if( !seen.Add(p) ) return;
        foreach( var f in p.fields )
            if( read_only ? f.read_only : f.write_only )
                acc.Add((p, f));
        foreach( var i in p.inherits )
            if( get(i) is Pack b )
                collect_direction_fields(b, read_only, acc, seen);
    }

    /// <summary>
    /// Gets or creates the direction projection of a shared pack.
    /// write=true → the pack a CLIENT may send (readOnly fields blocked);
    /// write=false → the pack a SERVER sends (writeOnly fields blocked).
    /// Returns null when the pack needs no projection in that direction.
    /// </summary>
    static Pack? ensure_projection(Pack src, bool write)
    {
        if( src.is_enum || src.is_typedef || src.HeaderFor != null ) return null;
        if( synthetic_client_sends.Contains(src) || synthetic_server_sends.Contains(src) ) return null; // cleaned in place

        var blocked = new List<(Pack owner, Field f)>();
        collect_direction_fields(src, read_only: write, blocked, []);
        if( blocked.Count == 0 ) return null;

        var name     = src.name + (write ? "_Write" : "_Read");
        var parent   = src.parent ?? root;
        var existing = parent.children.FirstOrDefault(c => c.name == name);
        if( existing != null ) return existing;

        var proj = new Pack { name = name, parent = parent };
        parent.children.Add(proj);
        proj.add_comment($"{(write ? "Write" : "Read")}-projection of {src.name} ({(write ? "client → server: readOnly" : "server → client: writeOnly")} fields blocked). " +
                         $"SSOT: the fields live in {src.name}; rename/retype there and this projection follows.");
        proj.raw_doc = string.Concat(blocked.Select(t => $"    /// <see cref=\"{t.owner}.{t.f.name}\"/>-\n"));
        proj.add_inherits(src.ToString().Replace('.', '/'));
        return proj;
    }

    static void apply_direction_semantics()
    {
        // 1. Synthetic packs the converter owns: remove fields that must not travel their direction.
        void strip(Pack p, bool read_only)
        {
            var gone = p.fields.Where(f => read_only ? f.read_only : f.write_only).Select(f => f.name).ToList();
            if( 0 < gone.Count )
            {
                p.fields.RemoveAll(f => read_only ? f.read_only : f.write_only);
                p.add_comment($"{(read_only ? "readOnly" : "writeOnly")} fields excluded here (wrong direction): {string.Join(", ", gone)}");
            }

            // An inherited shared pack is swapped for its direction projection.
            p.inherits = p.inherits
                          .Select(path => get(path) is Pack b && ensure_projection(b, write: read_only) is { } pr ?
                                              pr.ToString().Replace('.', '/') :
                                              path)
                          .ToHashSet();
        }

        foreach( var p in synthetic_client_sends ) strip(p, read_only: true);
        foreach( var p in synthetic_server_sends ) strip(p, read_only: false);

        // 2. Actor params referencing shared packs: re-point to the direction projection.
        void subst(Actor.Param? prm, bool write)
        {
            if( prm == null ) return;
            var tn = prm.GetTypeName();
            if( string.IsNullOrEmpty(tn) || tn == "NoArg" ) return;
            var pk = find_pack(tn);
            if( pk == null ) return;
            if( ensure_projection(pk, write) is { } pr ) prm.typeName = pr.ToString();
        }

        void walk(Actor n, bool webhook)
        {
            subst(n.request, write: !webhook);           // main: client sends the request; webhook: the SERVER pushes it
            foreach( var r in n.response ) subst(r, write: webhook);
            foreach( var c in n.callbackRPacks ) subst(c, write: false); // server push
            foreach( var c in n.callbackLPacks ) subst(c, write: true);  // client ack
            foreach( var c in n.children ) walk(c, webhook);
        }

        walk(root_actor, webhook: false);
        walk(root_webhook_actor, webhook: true);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Dashboard (Packs Inventory)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Records, per pack, the OpenAPI tags of every operation that transmits it.</summary>
    static void collect_dashboard_info(Actor rootNode)
    {
        void mark(Actor n, Actor.Param? prm)
        {
            if( prm == null ) return;
            var tn = prm.GetTypeName();
            if( string.IsNullOrEmpty(tn) || tn == "NoArg" || tn == "StandardErrors" ) return;
            pack_used.Add(tn);
            if( n.tags.Count == 0 ) return;
            if( !pack_tags.TryGetValue(tn, out var set) ) pack_tags[tn] = set = new SortedSet<string>();
            foreach( var t in n.tags ) set.Add(t);
        }

        void walk(Actor n)
        {
            if( n.IsOperation )
            {
                mark(n, n.request);
                foreach( var r in n.response ) mark(n, r);
                foreach( var c in n.callbackRPacks ) mark(n, c);
                foreach( var c in n.callbackLPacks ) mark(n, c);
            }

            foreach( var c in n.children ) walk(c);
        }

        walk(rootNode);
    }

    /// <summary>
    /// Builds the Packs Inventory block for the top of the file: one `&lt;see cref/&gt;` line per
    /// transmittable pack, alphabetized, pre-tagged with the OpenAPI tags of the operations using
    /// it — ready for KeepDoc/tag-based routing. IDs are assigned reactively by the system later.
    /// </summary>
    static string build_dashboard()
    {
        var lines = new List<string>();

        void walk(Pack p)
        {
            foreach( var c in p.children )
            {
                if( string.IsNullOrEmpty(c.Reference) )
                {
                    var full = c.ToString();
                    if( !c.is_enum && !c.is_typedef && c.HeaderFor == null &&
                        !full.StartsWith("components.formats")            && // TYPEDEF/metadata aliases, not payloads
                        (0 < c.fields.Count || 0 < c.inherits.Count || c.raw_doc != "" || pack_used.Contains(full)) )
                    {
                        var tags = pack_tags.TryGetValue(full, out var s) ?
                                       string.Join(" | ", s) :
                                       "";
                        // Project-name-qualified: the Dashboard doc sits on the project interface,
                        // so crefs resolve from the namespace scope (see AdhocProtocol.cs).
                        lines.Add($"        <see cref = '{ProjectName}.{full}'/>{(tags == "" ? "" : " " + xml_escape(tags))}");
                    }

                    walk(c);
                }
            }
        }

        walk(root);
        lines.Add($"        <see cref = '{ProjectName}.NoArg'/>");
        lines.Sort(StringComparer.Ordinal);
        return string.Join("\n", lines);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Link resolution (two-pass: first build all actors, then wire links)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Depth-first search for an actor whose name matches the given operationId.
    /// The root_actor sentinel itself is never returned.
    /// </summary>
    private static Actor? FindActorByOpId(Actor node, string opId)
    {
        if( node != root_actor &&
            node.name.Equals(brush(opId, ""), StringComparison.OrdinalIgnoreCase) )
            return node;

        foreach( var child in node.children )
        {
            var found = FindActorByOpId(child, opId);
            if( found != null ) return found;
        }

        return null;
    }

    /// <summary>
    /// Walk the actor tree and, for every actor that has pending link opIds,
    /// resolve them to fully-qualified C# paths, populate actor.links, and
    /// mark the target actor as forceExplicit so it always emits a referenceable
    /// Call state.
    /// </summary>
    private static void ResolveAllLinks(Actor node, string qualifiedPrefix)
    {
        if( node.pendingLinkOpIds.Count > 0 )
        {
            foreach( var (code, opIds) in node.pendingLinkOpIds )
            {
                var resolvedList = new List<string>();
                foreach( var opId in opIds )
                {
                    if( opId.StartsWith("$ref:") )
                    {
                        // operationRef link — convert slash path to dotted qualified name
                        var refPath = clean_path(opId.Substring(5)).Replace('/', '.');
                        resolvedList.Add($"{qualifiedPrefix}.{refPath}");
                    }
                    else
                    {
                        // operationId link — search the actor tree
                        var target = FindActorByOpId(root_actor, opId);
                        if( target != null )
                        {
                            target.forceExplicit = true; // must expose a named Call state
                            resolvedList.Add($"{qualifiedPrefix}.{target}");
                        }
                    }
                }

                if( resolvedList.Count > 0 ) node.links[code] = resolvedList;
            }

            node.pendingLinkOpIds.Clear();
        }

        foreach( var child in node.children )
            ResolveAllLinks(child, qualifiedPrefix);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Attribute helpers
    // ─────────────────────────────────────────────────────────────────────────
    public static Dictionary<string, string> attributes = [];

    public static string add_attribute(string name, string[]? args_name, string[] args_values,
                                       bool   AllowMultiple = false)
    {
        if( !attributes.ContainsKey(name) )
            attributes[name] = $@" {(AllowMultiple ? "[AttributeUsage(AttributeTargets.All , AllowMultiple = true)]\n" : "")} public class {name}Attribute : Attribute{{ public {name}Attribute( {string.Join(',', args_values.Select((a, i) => $"{(a.Length > 0 && a[0] == '\"' ? "string" : a.Contains('.') ? "double" : "long")} {(args_name == null || args_name.Length <= i ? $"arg{i}" : args_name[i])}"))} ) {{ }} }} ";

        // String args arrive wrapped in plain quotes; re-emit as verbatim @"..." with inner quotes doubled,
        // so values containing quotes or newlines stay valid C#.
        return args_values.All(a => a == "\"\"" || a == "") ?
                   $"[{name}]\n" :
                   $"[{name}( {string.Join(", ", args_values.Select(a => a.Length > 1 && a[0] == '"' ? "@\"" + a[1..^1].Replace("\"", "\"\"") + "\"" : a))} )]\n";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Lazy TYPEDEF alias for text-era formats whose binary representation is native.
    //  Example: `format: uuid` → 16 fixed bytes on the wire, with a [Validate] hint
    //  carrying the canonical text regex + MaxLength so GUIs can render/validate.
    //  Only emitted once per alias name.
    // ─────────────────────────────────────────────────────────────────────────
    public static string format_alias(string aliasName, string wireType, string? validationRegex, int textMaxLength)
    {
        var alias = Pack.get_or_new($"components/formats/{aliasName}");
        if( alias.fields.Count == 0 )
        {
            var fld = new Field(alias, "TYPEDEF", wireType);
            fld.attributes = emit_validate(validationRegex, textMaxLength) + fld.attributes;
        }

        return alias.ToString()!;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Duration alias.
    //
    //  OpenAPI `format: duration` (ISO 8601 duration) and `format: time` (time-of-day)
    //  are elapsed-time semantics. The correct AdHoc construct is `Duration` (interface
    //  in org.unirail.Meta) — implemented on a struct/class. Bare `TimeSpan` is NOT a
    //  transmittable field type; it only appears as a literal value inside the
    //  metadata interfaces' precision/interval properties.
    //
    //  We synthesize a single `DurationDefault : Duration { }` pack the first time the
    //  alias is requested. With no overrides the Duration interface defaults apply:
    //  precision = 1 s, max = (1L << 53) - 1 → 7 bytes on the wire. Callers who care
    //  about wire size can swap the field type for a tighter Duration subclass later.
    //  The class name intentionally differs from `Duration` to avoid colliding with the
    //  `org.unirail.Meta.Duration` interface that the `using` directive imports.
    // ─────────────────────────────────────────────────────────────────────────
    public static string duration_alias()
    {
        var alias = Pack.get_or_new("components/formats/DurationDefault");
        if( alias.inherits.Count == 0 ) alias.add_inherits("Duration");
        return alias.ToString()!;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Pick a single representative schema from an OpenAPI Content map.
    //  AdHoc is one binary format — `application/json`, `application/xml` etc. are HTTP-era
    //  serialization labels we don't carry over. Prefer JSON, then any, else null.
    // ─────────────────────────────────────────────────────────────────────────
    public static IOpenApiSchema? pick_schema(IDictionary<string, IOpenApiMediaType>? content)
    {
        if( content == null || content.Count == 0 ) return null;
        if( content.TryGetValue("application/json", out var json) && json.Schema != null ) return json.Schema;
        foreach( var (_, m) in content )
            if( m.Schema != null ) return m.Schema;
        return null;
    }

    /// <summary>`type: string, format: binary|byte` — a raw byte payload (file upload/download).</summary>
    public static bool is_binary_schema(IOpenApiSchema? s) =>
        s?.Type?.HasFlag(JsonSchemaType.String) == true && s.Format is "binary" or "byte";

    /// <summary>
    /// Body-level raw payload → a `File` conduit field: bytes are piped source→socket with no
    /// in-memory buffering (AdHoc Direct Transfer) instead of being crammed into a Binary array.
    /// Also fires when the media type is binary-ish (octet-stream etc.) but declares no schema.
    /// </summary>
    public static Field add_file_conduit(Pack dst, string name, string? comment)
    {
        var fld = new Field(dst, name, "File");
        fld.attributes = "[S(1_000_000_000 /*TODO: set the real cap*/)] " + fld.attributes;
        fld.add_comment(comment);
        return fld;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  [Validate(...)] — GUI/form validation hint. Not wire semantics.
    //  Emitted for string `pattern` on regular schema fields, and for TYPEDEF
    //  aliases derived from text-based formats (uuid, ipv4, ipv6) where the
    //  wire type is binary but the GUI still needs to validate the text form.
    // ─────────────────────────────────────────────────────────────────────────
    public static string emit_validate(string? regex, int? maxLength)
    {
        if( string.IsNullOrEmpty(regex) && !maxLength.HasValue ) return "";

        if( !attributes.ContainsKey("Validate") )
            attributes["Validate"] = "[AttributeUsage(AttributeTargets.All)] public class ValidateAttribute : Attribute { public string Regex { get; set; } = \"\"; public long MaxLength { get; set; } = -1; }";

        var parts = new List<string>();
        if( !string.IsNullOrEmpty(regex) ) parts.Add($"Regex = @\"{regex.Replace("\"", "\"\"")}\"");
        if( maxLength.HasValue ) parts.Add($"MaxLength = {maxLength.Value}");
        return $"[Validate({string.Join(", ", parts)})]\n";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Entity (base class for Pack and Field)
    // ─────────────────────────────────────────────────────────────────────────
    public abstract class Entity{
        public Pack?  parent;
        public string name = "";

        public abstract void write(StringBuilder dst);

        public override string ToString()
        {
            void scan(Entity src, StringBuilder dst)
            {
                if( src.parent == null || src.parent == root ) dst.Append(src.name);
                else
                {
                    scan(src.parent!, dst);
                    dst.Append('.').Append(src.name);
                }
            }

            scan(this, tmp.Clear());
            return tmp.ToString();
        }

        public string comment    = "";
        public string attributes = "";

        public Entity add_comment(string? comment)
        {
            if( !string.IsNullOrEmpty(comment) ) this.comment += comment + "\n";
            return this;
        }

        public Entity add_attributes(string attributes)
        {
            this.attributes += attributes + "\n";
            return this;
        }
    }

    // OpenAPI descriptions are CommonMark/HTML — raw '<' and '&' would corrupt the C# XML doc
    // comments the AdHoc parser reads (the doc pool for KeepDoc filters). Escape them.
    static string xml_escape(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;");

    static string _comment(string? comment, string indent = "    ")
    {
        if( string.IsNullOrWhiteSpace(comment?.Trim()) || comment.Trim().StartsWith("(empty)") )
            return "";

        var lines = comment.Trim().Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var sb    = new StringBuilder();

        sb.AppendLine($"{indent}/// <summary>");
        foreach( var line in lines )
        {
            // Avoid empty lines having trailing spaces after ///
            var trimmedLine = xml_escape(line.Trim());
            sb.AppendLine($"{indent}/// {(string.IsNullOrEmpty(trimmedLine) ? "" : trimmedLine)}");
        }

        sb.AppendLine($"{indent}/// </summary>");

        return sb.ToString();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Field
    // ─────────────────────────────────────────────────────────────────────────
    public class Field : Entity{
        public IOpenApiSchema? Schema;
        public bool            optional;
        public bool            has_Set_type;
        public bool            has_Map_type;
        public bool            read_only;  // OpenAPI readOnly: present only in server→client data
        public bool            write_only; // OpenAPI writeOnly: present only in client→server data
        public string          inline_type = "";

        public Field(Pack pack, string name, string inline_type)
        {
            parent           = pack;
            this.name        = brush(name, pack.name);
            this.inline_type = inline_type;
            pack.fields.Add(this);
        }

        public Field(Pack pack, string name, IOpenApiSchema? schema, bool has_Map_type = false)
        {
            parent    = pack;
            this.name = brush(name, pack.name);
            Schema    = schema;

            if( schema != null )
            {
                has_Set_type      = schema.UniqueItems ?? false;
                this.has_Map_type = has_Map_type;

                var is_array  = schema.Type?.HasFlag(JsonSchemaType.Array) == true;
                var is_binary = is_binary_schema(schema);

                // ── Size caps → [D] ─────────────────────────────────────────
                // string        → [D(+chars)]           (intrinsic length)
                // binary blob   → [D(bytes)]            (Binary[,,] list cap; default 255 is far too small)
                // array         → [D(+itemChars, count)] combined element-length + item-count dims
                // Set           → [D(+count)] + [Key: D(+chars)] for string elements
                // Map           → [D(+maxProperties)]
                if( is_binary )
                    attributes += $"[D({(schema.MaxLength.HasValue ? schema.MaxLength.ToString() : "1_000_000 /*TODO: no size in the spec — set the real cap*/")})]\n";
                else if( is_array )
                {
                    if( has_Set_type )
                    {
                        if( schema.MaxItems != null ) attributes            += $"[D(+{schema.MaxItems})]\n";
                        if( schema.Items?.MaxLength != null ) attributes    += $"[Key: D(+{schema.Items.MaxLength})]\n";
                    }
                    else
                    {
                        var dims = new List<string>();
                        if( schema.Items?.MaxLength != null ) dims.Add($"+{schema.Items.MaxLength}");
                        if( schema.MaxItems != null ) dims.Add(schema.MaxItems.ToString()!);
                        if( 0 < dims.Count ) attributes += $"[D({string.Join(", ", dims)})]\n";
                    }
                }
                else if( has_Map_type )
                {
                    if( schema.MaxProperties != null ) attributes += $"[D(+{schema.MaxProperties})]\n";
                }
                else if( schema.MaxLength.HasValue ) attributes += $"[D(+{schema.MaxLength})]\n";

                if( schema.Default != null )
                {
                    if( !OpenApi_To_AdHoc_Converter.attributes.ContainsKey("Default") )
                        OpenApi_To_AdHoc_Converter.attributes["Default"] = "public class DefaultAttribute : Attribute{ public DefaultAttribute(double value){} public DefaultAttribute(long value){} public DefaultAttribute(string value){} public DefaultAttribute(bool value){} }";

                    if( schema.Default is JsonValue jValue )
                        if( jValue.TryGetValue<long>(out var lVal) ) attributes        += $"[Default({lVal})]\n";
                        else if( jValue.TryGetValue<double>(out var dVal) ) attributes += $"[Default({dVal.ToString(CultureInfo.InvariantCulture)})]\n";
                        else if( jValue.TryGetValue<bool>(out var bVal) ) attributes   += $"[Default({bVal.ToString().ToLower()})]\n";
                        else if( jValue.TryGetValue<string>(out var sVal) )
                            attributes += $"[Default(@\"{sVal.Replace("\"", "\"\"")}\")]\n";
                }

                // ── Numeric bounds ──────────────────────────────────────────
                // Both bounds     → [MinMax(min, max)]  (generator picks smallest storage / bit-packs)
                // Only minimum    → [A(min)]  hard floor + varint  (integers only)
                // Only maximum    → [V(max)]  hard ceiling + varint (integers only)
                // One-sided float → constraint comment (MinMax needs both bounds)
                {
                    var is_int = schema.Type?.HasFlag(JsonSchemaType.Integer) == true;

                    // OpenAPI 3.1: exclusiveMinimum/Maximum carry the number itself. Fold to inclusive for integers.
                    var min = schema.Minimum;
                    var max = schema.Maximum;
                    if( min == null && schema.ExclusiveMinimum != null )
                        min = is_int && long.TryParse(schema.ExclusiveMinimum, NumberStyles.Integer, CultureInfo.InvariantCulture, out var xm) ?
                                  (xm + 1).ToString() :
                                  schema.ExclusiveMinimum;
                    if( max == null && schema.ExclusiveMaximum != null )
                        max = is_int && long.TryParse(schema.ExclusiveMaximum, NumberStyles.Integer, CultureInfo.InvariantCulture, out var xM) ?
                                  (xM - 1).ToString() :
                                  schema.ExclusiveMaximum;

                    if( min != null && max != null ) attributes += $"[MinMax({min}, {max})]\n";
                    else if( min != null )
                        if( is_int ) attributes += $"[A({min})]\n"; // values start at min; varint favors small values
                        else add_comment($"constraint: minimum {min}");
                    else if( max != null )
                        if( is_int ) attributes += $"[V({max})]\n"; // values capped at max
                        else add_comment($"constraint: maximum {max}");
                }

                // pattern → [Validate(Regex=...)] — GUI/form validation hint only.
                // Text-era validation (minLength, multipleOf, exclusiveMin/Max, format name, minItems)
                // is dropped; AdHoc is a binary protocol.
                if( !string.IsNullOrEmpty(schema.Pattern) ) attributes += emit_validate(schema.Pattern, null);

                // readOnly/writeOnly: markers stay on the source pack (SSOT); a later pass
                // builds direction projections (_Read/_Write) and strips these fields from
                // synthetic request/response packs.
                if( read_only  = schema.ReadOnly )  attributes += add_attribute("ReadOnly",  [], []);
                if( write_only = schema.WriteOnly ) attributes += add_attribute("WriteOnly", [], []);

                // deprecated: use schema.Description as Obsolete message when present
                if( schema.Deprecated )
                {
                    var msg = string.IsNullOrEmpty(schema.Description) ?
                                  "Deprecated" :
                                  schema.Description;
                    attributes += add_attribute("Obsolete", ["Message"], [$"\"{msg}\""]);
                }

                if( 0 < schema.Extensions?.Count )
                    foreach( var (ek, ev) in schema.Extensions )
                        attributes += add_attribute("Extension", ["Key", "Value"], [$"\"{ek}\"", $"\"{ev}\""], true);

                // Enum on the schema itself, or on the ITEMS of an array/Set (previously lost).
                var enum_src = 0 < schema.Enum?.Count ? schema :
                               is_array && 0 < schema.Items?.Enum?.Count ? schema.Items :
                               null;
                if( enum_src != null )
                {
                    var en = Pack.get_enum(pack.ToString().Replace('.', '/') + "/" + cap(this.name), enum_src.Enum!);
                    if( string.IsNullOrEmpty(en.comment) ) en.comment = enum_src.Description ?? "";
                    inline_type = enum_src == schema ? en.ToString() :
                                  has_Set_type       ? en.ToString() :                // write() wraps it in Set<>
                                                       en.ToString() + "[,,]";        // dynamic list of enums
                }

                if( schema.Example != null ) add_comment($"examples: {schema.Example}");
                if( schema.Examples != null && 0 < schema.Examples.Count ) add_comment($"examples: {string.Join(", ", schema.Examples.Select(e => e.ToString()))}");
            }

            pack.fields.Add(this);
        }

        public string value = "";

        public string get_type_string() => type(Schema, new HashSet<IOpenApiSchema>());

        public string type(IOpenApiSchema? schema, HashSet<IOpenApiSchema> visited)
        {
            if( schema == null ) return "Binary[,,]"; // unknown/untyped schema — safest transportable fallback
            if( !visited.Add(schema) )
                return schema is OpenApiSchemaReference sh && get(GetReferencePath(sh.Reference)) is Pack pk ?
                           pk.ToString() :
                           "Binary[,,]";
            try
            {
                if( !string.IsNullOrEmpty(inline_type) ) return inline_type;

                if( schema is OpenApiSchemaReference sh2 )
                    switch( get(GetReferencePath(sh2.Reference)) )
                    {
                        case Field fld: return fld.type(fld.Schema!, visited);
                        case Pack pk:   return pk.ToString()!;
                        case null:
                            Console.Out.WriteLine($"ERROR: Unknown type: {GetReferencePath(sh2.Reference)}");
                            return "Binary[,,]"; // unknown/untyped schema — safest transportable fallback
                    }

                if( !schema.Type.HasValue ) return "Binary[,,]"; // unknown/untyped schema — safest transportable fallback

                if( schema.Type.Value.HasFlag(JsonSchemaType.Integer) )
                    return schema.Format == "int64" ?
                               "long" :
                               "int";
                if( schema.Type.Value.HasFlag(JsonSchemaType.Number) )
                    return schema.Format == "float" ?
                               "float" :
                               "double";

                if( schema.Type.Value.HasFlag(JsonSchemaType.String) )
                    if( string.IsNullOrEmpty(schema.Format) ) return "string";
                    else
                        switch( schema.Format )
                        {
                            case "date-time":
                            case "date": return "DateTime";
                            case "time":
                            case "duration": return duration_alias();
                            case "binary":
                            case "byte": return "Binary[,,]"; // raw in-memory blob; the [D] cap is emitted by the ctor
                            case "uuid":     return format_alias("Uuid", "[D(16)] Binary[]", // constant-length 16-byte array
                                                                  @"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$", 36);
                            case "ipv4":     return format_alias("IPv4", "uint",
                                                                  @"^(25[0-5]|2[0-4]\d|[01]?\d\d?)(\.(25[0-5]|2[0-4]\d|[01]?\d\d?)){3}$", 15);
                            case "ipv6":     return format_alias("IPv6", "[D(16)] Binary[]", null, 39);
                            case "mac":      return format_alias("Mac",  "[D(6)] Binary[]",
                                                                  @"^([0-9a-fA-F]{2}:){5}[0-9a-fA-F]{2}$", 17);
                            default:
                                return "string";
                        }


                if( schema.Type.Value.HasFlag(JsonSchemaType.Boolean) ) return "bool";
                if( schema.Type.Value.HasFlag(JsonSchemaType.Array) )
                {
                    // JSON arrays are variable-length → dynamic list `[,,]`.
                    // Constant-length `[]` only when minItems == maxItems pins the size.
                    var const_size = schema.MinItems != null && schema.MinItems == schema.MaxItems;
                    return $"{type(schema.Items, visited)}[{(const_size ? "" : ",,")}]";
                }
                if( schema.Type.Value.HasFlag(JsonSchemaType.Object) )
                {
                    if( schema.AdditionalProperties != null )
                        return $"Map< string , {type(schema.AdditionalProperties, visited)}>";

                    // Inline (anonymous) object — synthesize a nested pack next to the owning pack
                    // instead of degrading to raw bytes.
                    if( 0 < schema.Properties?.Count )
                    {
                        var p = Pack.get_or_new(parent!.ToString().Replace('.', '/') + "/" + cap(name));
                        if( p.fields.Count == 0 && p.children.Count == 0 ) // idempotent: type() runs more than once
                        {
                            add_properties(schema, p);
                            p.add_comment(schema.Description);
                        }

                        return p.ToString();
                    }
                }

                return "Binary[,,]"; // unknown/untyped schema — safest transportable fallback
            }
            finally { visited.Remove(schema); }
        }

        public override void write(StringBuilder dst)
        {
            if( Schema == null )
            {
                // TYPEDEF alias field (no schema, but an explicit inline_type)
                if( !string.IsNullOrEmpty(inline_type) )
                {
                    dst.Append($"    {_comment(comment)}    {attributes}{inline_type} {name};\n");
                    return;
                }

                // Enum value
                dst.Append(name);
                if( value != "" ) dst.Append(" = ").Append(value);
                dst.Append(',').AppendLine();
                return;
            }

            var V = "";
            var T = get_type_string();
            if( has_Set_type ) // uniqueItems: the SET replaces the array — its element type is the array's item type
                T = $"Set<{(inline_type != ""    ? inline_type :
                            Schema?.Items != null ? type(Schema.Items, new HashSet<IOpenApiSchema>()) :
                                                    T)}>";
            else if( has_Map_type )
                if( T.Contains("Map<") )
                {
                    V = $"class {name}_V {{ {T} V;   }} ";
                    T = $"Map< string , {name}_V>";
                }
                else T = $"Map< string , {T}>";

            dst.Append($"""
                            {_comment(comment + "\n" + Schema.Description)}
                            {attributes}{T} {(optional ? "?" : "")} {name};
                            {V}
                        """);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Pack
    // ─────────────────────────────────────────────────────────────────────────
    public class Pack : Entity{
        public List<Field>     fields    = [];
        public List<Pack>      children  = [];
        public HashSet<string> inherits  = [];
        public string          Reference = "";
        public bool            is_enum;
        public string?         HeaderFor;
        public string          raw_doc = ""; // verbatim `///` lines (SSOT projection <see/> refs) — never wrapped in <summary>

        public bool is_typedef => fields.Count == 1 && fields[0].name == "TYPEDEF";

        public void add_inherits(string path) => inherits.Add(path);

        static readonly Dictionary<string, Pack> enum_registry = new();

        public static Pack get_enum(string ref_path, IEnumerable<JsonNode> Enum)
        {
            // Deduplicate identical enums: the same member list (values and order) reuses the
            // first declaration instead of spawning a copy per field (e.g. `status` enums).
            var signature = string.Join("\u0001", Enum.Select(n => n?.ToString() ?? ""));
            if( enum_registry.TryGetValue(signature, out var existing) ) return existing;

            var en = get_or_new(ref_path);
            enum_registry[signature] = en;
            en.is_enum               = true;
            foreach( var fld in Enum )
                switch( fld )
                {
                    case JsonValue jv when jv.TryGetValue<long>(out var lv):
                        new Field(en, "x" + lv, (string)null!) { value = lv.ToString() }; break;
                    case JsonValue jv when jv.TryGetValue<double>(out var dv):
                        new Field(en, "x" + dv.ToString(CultureInfo.InvariantCulture), (string)null!)
                            { value = dv.ToString(CultureInfo.InvariantCulture) }; break;
                    case JsonValue jv when jv.TryGetValue<bool>(out var bv):
                        new Field(en, "x" + bv, (string)null!); break;
                    case JsonValue jv:
                        if( jv.TryGetValue<string>(out var sv) ) new Field(en, sv, (string)null!);
                        break;
                }

            return en;
        }

        public static Pack get_or_new(string ref_path)
        {
            var p = root;
            foreach( var n in clean_path(ref_path).Split('/') )
            {
                var name = brush(n, p.name);
                var next = p.children.FirstOrDefault(c => c.name == name);
                if( next == null ) p.children.Add(next = new Pack { name = name, parent = p });
                p = next;
            }

            return p;
        }

        public override void write(StringBuilder dst)
        {
            if( this != root )
            {
                dst.AppendLine()
                   .Append(_comment(comment))
                   .Append(raw_doc)
                   .Append(string.Join('\n', attributes))
                   .AppendLine();
                dst.Append($"public {(is_enum ? "enum" : "class")} {name}");

                if( inherits.Count == 1 )
                {
                    var inheritPath = inherits.First();
                    var from        = get(inheritPath);
                    if( from is Field fld )
                    {
                        dst.Append("{\n");
                        fld.write(dst);
                    }
                    else
                        // When `get()` cannot resolve the path (e.g. the inherit names an external
                        // type like `Duration` from `org.unirail.Meta`, imported via `using`), fall
                        // back to the literal token rather than emitting an empty parent name.
                        dst.Append($" : {from?.ToString() ?? inheritPath} {{\n");
                }
                else if( inherits.Count > 1 )
                {
                    // OpenAPI `allOf` with multiple refs → AdHoc multi-inheritance via `_<(A,B,C)>`.
                    var parents = string.Join(", ", inherits.Select(i => get(i)?.ToString() ?? i));
                    dst.Append($" : _<({parents})> {{\n");
                }
                else
                    dst.Append($" {(HeaderFor == null ? "" : $": HeaderFor<{HeaderFor}>")} {{\n");
            }

            if( is_enum && fields.Count < 2 ) new Field(this, "one_more_field", (string)null!);

            // Snapshot `fields` and `children` before iterating: Field.write → get_type_string
            // may trigger format_alias(), which lazily creates TYPEDEF alias Packs elsewhere
            // in the tree. Without snapshotting we'd collide with our own enumeration.
            foreach( var fld in fields.ToList() ) fld.write(dst);
            foreach( var pack in children.Where(p => string.IsNullOrEmpty(p.Reference)).ToList() ) pack.write(dst);

            if( this != root ) dst.Append("\n}\n");
        }
    }

    public static Pack root = new();

    public class Actor{
        // ── Param ─────────────────────────────────────────────────────────
        /**
         * Represents a typed slot in a request or response.
         *
         * typeName:  when set, the literal type token to emit (e.g. "components.schemas.Pet").
         * pack:      when set, the Pack whose name/reference provides the type.
         *
         * GetTypeName() resolves whichever is relevant.
         */
        public class Param{
            public Pack?   pack;
            public string? typeName;
            public string  comment  = "";
            public string  httpCode = "";

            /// <summary>Construct from a pre-resolved type name string.</summary>
            public Param(string typeName) => this.typeName = typeName;

            /// <summary>Construct from a Pack (type name resolved at write time).</summary>
            public Param(Pack pack) => this.pack = pack;

            /// <summary>Returns the AdHoc type token for this param.</summary>
            public string GetTypeName()
            {
                if( !string.IsNullOrEmpty(typeName) ) return typeName!;
                if( pack == null ) return "NoArg";

                // If the pack is just an alias for another schema, follow the reference.
                if( !string.IsNullOrEmpty(pack.Reference) )
                {
                    var resolved = get(pack.Reference) as Pack;
                    if( resolved != null ) return resolved.ToString();
                }

                return pack.ToString();
            }
        }

        // ── Actor fields ───────────────────────────────────────────────────
        public Actor?       parent;
        public string       name = "";
        public Param?       request;
        public List<Param>  response   = [];
        public List<Actor>  children   = [];
        public string       comment    = "";
        public string       attributes = "";
        public List<string> tags       = []; // OpenAPI operation tags — propagated to the Dashboard

        public Dictionary<string, List<string>> links = new();
        public bool                             HasLinks => links.Count > 0;
        public bool                             forceExplicit    = false;
        public Dictionary<string, List<string>> pendingLinkOpIds = new();

        /// <summary>True when this node carries request/response data (leaf HTTP operation).</summary>
        public bool IsOperation => request != null || response.Count > 0;

        public static Actor get_or_new(string ref_path) => get_or_new(ref_path, root_actor);

        public static Actor get_or_new(string ref_path, Actor rootOverride)
        {
            var a = rootOverride;
            foreach( var n in clean_path(ref_path).Split('/') )
            {
                var name = brush(n, a.name);
                var next = a.children.FirstOrDefault(c => c.name == name);
                if( next == null ) a.children.Add(next = new Actor { name = name, parent = a });
                a = next;
            }

            return a;
        }

        public Actor add_comment(string? comment)
        {
            if( !string.IsNullOrEmpty(comment) ) this.comment += comment + "\n";
            return this;
        }

        public override string ToString()
        {
            void scan(Actor src, StringBuilder sb)
            {
                if( src.parent == null || src.parent == root_actor || src.parent == root_webhook_actor ) sb.Append(src.name);
                else
                {
                    scan(src.parent!, sb);
                    sb.Append('.').Append(src.name);
                }
            }

            scan(this, tmp.Clear());
            return tmp.ToString();
        }

        // ── write ──────────────────────────────────────────────────────────
        public void write(StringBuilder dst)
        {
            // Root: just recurse into children (no wrapper)
            if( this == root_actor || this == root_webhook_actor )
            {
                foreach( var child in children ) child.write(dst);
                return;
            }

            if( HasCallbacks || HasLinks || forceExplicit ) WriteAsExplicitActor(dst);
            else if( IsOperation )
                WriteAsShorthand(dst);
            else
                WriteAsContainer(dst);
        }

        // A link target's states are GRAFTED (copied) into the referencing actor's FSM, and state
        // names must be unique within one actor — so every state name is suffixed with its actor's
        // name (`Call_getPet`, not `Call`), letting any actor graft any other without collisions.
        static string CallState(string actorName) => $"Call_{actorName}";

        void WriteAsExplicitActor(StringBuilder dst)
        {
            dst.Append(_comment(comment, "        "));
            if( !string.IsNullOrEmpty(attributes) ) dst.AppendLine($"        {attributes.Trim()}");
            dst.AppendLine($"        public interface {name} : Actor {{");
            dst.AppendLine($"            int MaxActiveInstances => UNLIMITED;");

            // State 1: Call — transitional L branch to Return
            var reqType = request?.GetTypeName() ?? "NoArg";
            dst.AppendLine($"            [L____________<Return_{name}, {reqType}>]");
            dst.AppendLine($"            struct {CallState(name)} {{ }}");

            // State 2: Return — one attribute per distinct target (packs grouped per target)
            var returnGroups = new Dictionary<string, List<string>>();
            foreach( var resp in response )
            {
                var code = resp.httpCode;

                string target;
                if( links.TryGetValue(code, out var codeLinks) && codeLinks.Count > 0 )
                    target = codeLinks.Count == 1 && !HasCallbacks ?
                                 $"{codeLinks[0]}.{CallState(codeLinks[0].Split('.')[^1])}" :
                                 $"Links_{code}_{name}";
                else
                    target = HasCallbacks ?
                                 $"CallbackState_{name}" :
                                 "End";

                if( !returnGroups.TryGetValue(target, out var list) )
                    returnGroups[target] = list = new List<string>();
                list.Add(resp.GetTypeName());
            }

            foreach( var (target, packs) in returnGroups )
            {
                var packsExpr = packs.Count == 1 ?
                                    packs[0] :
                                    "(" + string.Join(", ", packs) + ")";
                dst.AppendLine($"            [____________R<{target}, {packsExpr}>]");
            }

            dst.AppendLine($"            struct Return_{name} {{ }}");

            // State 3: Link-choice states — only emitted when a code has MULTIPLE link targets,
            // or when a single link coexists with a callback state.
            foreach( var (code, targetActors) in links )
            {
                if( targetActors.Count == 1 && !HasCallbacks ) continue; // already inlined in Return above

                foreach( var target in targetActors )
                    dst.AppendLine($"            [L____________<{target}.{CallState(target.Split('.')[^1])}, NoArg>]");
                dst.AppendLine($"            [L____________<{(HasCallbacks ? $"CallbackState_{name}" : "End")}, NoArg>]");
                dst.AppendLine($"            struct Links_{code}_{name} {{ }}");
            }

            // State 4: Persistent Callback State (Followers + graceful close)
            if( HasCallbacks )
            {
                if( callbackLPacks.Count > 0 )
                    dst.AppendLine($"            [l____________<{ToTuple(callbackLPacks)}>]");
                if( callbackRPacks.Count > 0 )
                    dst.AppendLine($"            [____________r<{ToTuple(callbackRPacks)}>]");
                dst.AppendLine($"            [L____________<End, NoArg>]"); // Allow client to close session
                dst.AppendLine($"            struct CallbackState_{name} {{ }}");
            }

            dst.AppendLine("        }");
            dst.AppendLine();
        }

        // ── WriteAsContainer ───────────────────────────────────────────────
        /**
         * Path-segment node: emitted as a nested public interface containing
         * its children (which may be shorthand operations or further containers).
         */
        private void WriteAsContainer(StringBuilder dst)
        {
            dst.Append(_comment(comment));
            if( !string.IsNullOrEmpty(attributes) ) dst.AppendLine(attributes);
            dst.AppendLine($"        public interface {name} {{");
            foreach( var child in children ) child.write(dst);
            dst.AppendLine("        }");
            dst.AppendLine();
        }


        private void WriteAsShorthand(StringBuilder dst)
        {
            // 1. Doc-comment for the operation
            if( !string.IsNullOrEmpty(comment) ) { dst.Append(_comment(comment, "            ")); }

            // Attributes (Tags, SecurityRequirement, etc.)
            if( !string.IsNullOrEmpty(attributes) )
                dst.AppendLine($"            {attributes.Trim()}");

            // 2. Return tuple (Responses)
            var retSb = new StringBuilder();
            retSb.Append("(L____________");
            foreach( var rp in response )
            {
                retSb.AppendLine(",");
                if( !string.IsNullOrEmpty(rp.comment) )
                {
                    // Internal parameter comments also use ///
                    retSb.Append($"                /// <summary>{xml_escape(rp.comment.Trim())}</summary>\n                ");
                }
                else { retSb.Append("                "); }

                retSb.Append(rp.GetTypeName());
            }

            retSb.Append(')');

            // ... rest of method (request params) ...
            var reqStr = request != null ?
                             $"{request.GetTypeName()} req" :
                             "NoArg _";
            dst.AppendLine($"            {retSb} {name}({reqStr});");
            dst.AppendLine();
        }

        public List<Param> callbackLPacks = new(); // Packs the Client (Left) can send in Callback state
        public List<Param> callbackRPacks = new(); // Packs the Server (Right) can send in Callback state

        public bool HasCallbacks => callbackLPacks.Count > 0 || callbackRPacks.Count > 0;

        // Helper to format a list of Params into an AdHoc type string
        private string ToTuple(List<Param> list)
        {
            switch( list.Count )
            {
                case 0:  return "NoArg";
                case 1:  return list[0].GetTypeName();
                default: return "(" + string.Join(", ", list.Select(p => p.GetTypeName())) + ")";
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Statics
    // ─────────────────────────────────────────────────────────────────────────
    public static Actor         root_actor         = new() { name = "", parent = null };
    public static Actor         root_webhook_actor = new() { name = "", parent = null };
    public static StringBuilder hosts      = new();

    public static void read_servers(IList<OpenApiServer>? Servers)
    {
        // OpenAPI `servers` are alternative base URLs of the SAME API (prod/staging/regions) —
        // they are ONE AdHoc host carrying one [Server(...)] attribute per URL, not N hosts
        // (an extra host would need its own Connection and duplicate the whole protocol).

        // 1. SUMMARY
        hosts.AppendLine("\n    /// <summary>");
        hosts.AppendLine("    /// The API server. Server Definition: https://swagger.io/specification/#server-object");
        if( Servers != null )
            foreach( var server in Servers )
            {
                hosts.AppendLine($"    /// {xml_escape(server.Url ?? "")}");
                if( !string.IsNullOrEmpty(server.Description) )
                    foreach( var line in server.Description.Split('\n') )
                        hosts.AppendLine($"    ///     {xml_escape(line.Trim())}");

                if( server.Variables?.Count > 0 )
                    foreach( var (varName, variable) in server.Variables )
                    {
                        hosts.AppendLine($"    ///     Variable '{xml_escape(varName)}': https://swagger.io/specification/#server-variable-object");
                        if( !string.IsNullOrEmpty(variable.Description) )
                            foreach( var line in variable.Description.Split('\n') )
                                hosts.AppendLine($"    ///     {xml_escape(line.Trim())}");
                    }
            }

        hosts.AppendLine("    /// </summary>");
        hosts.AppendLine("    ///<see cref = 'InTS'/>   implementation in TypeScript");
        hosts.AppendLine("    ///<see cref = 'InCS'/>   implementation in C#");
        hosts.AppendLine("    ///<see cref = 'InJAVA'/> implementation in JAVA");
        hosts.AppendLine("    ///<see cref = 'InCPP'/>  implementation in C++");
        hosts.AppendLine("    ///<see cref = 'InRS'/>   implementation in RUST");
        hosts.AppendLine("    ///<see cref = 'InGO'/>   implementation in GO");

        // 2. ATTRIBUTES — one [Server] per URL, [ServerVariable] keyed by its server's URL
        if( Servers != null )
            foreach( var server in Servers )
            {
                hosts.Append(add_attribute("Server", ["Url", "Description"],
                                           [$"\"{server.Url}\"", $"\"{server.Description}\""], true));

                if( server.Extensions?.Count > 0 )
                    foreach( var (ek, ev) in server.Extensions )
                        hosts.Append(add_attribute("Extension", ["Key", "Value"], [$"\"{ek}\"", $"\"{ev}\""], true));

                if( server.Variables?.Count > 0 )
                    foreach( var (varName, variable) in server.Variables )
                    {
                        var enumList = (variable.Enum?.Count > 0) ?
                                           string.Join("|", variable.Enum) :
                                           "";

                        hosts.Append(add_attribute("ServerVariable",
                                                   ["ServerUrl", "Name", "Default", "Description", "Enum"],
                                                   [
                                                       $"\"{server.Url}\"", $"\"{varName}\"", $"\"{variable.Default}\"",
                                                       $"\"{variable.Description}\"", $"\"{enumList}\""
                                                   ], true));

                        if( variable.Extensions?.Count > 0 )
                            foreach( var (vk, vv) in variable.Extensions )
                                hosts.Append(add_attribute("Extension", ["Key", "Value"], [$"\"{vk}\"", $"\"{vv}\""], true));
                    }
            }

        // 3. STRUCT DEFINITION — exactly one host
        hosts.AppendLine("    struct Server : Host { }");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Reference / name utilities
    // ─────────────────────────────────────────────────────────────────────────
    static string clean_path(string refPath) => refPath
                                                .Replace("{", "I__").Replace("}", "__I")
                                                .Replace(":", "").Replace('-', '_')
                                                .Replace("[", "").Replace("]", "").Replace(" ", "_")
                                                .TrimStart('#', '/').TrimEnd('/');

    static StringBuilder tmp = new();

    public static object? get(string ref_path) => get(ref_path, new HashSet<string>());

    private static object? get(string ref_path, HashSet<string> visited)
    {
        if( !visited.Add(ref_path) )
        {
            var p_nr = root;
            foreach( var n in clean_path(ref_path).Split('/') )
            {
                var name2 = brush(n, p_nr.name);
                p_nr = p_nr.children.FirstOrDefault(c => c.name == name2)!;
                if( p_nr == null ) return null;
            }

            return p_nr;
        }

        var p    = root;
        var path = clean_path(ref_path).Split('/');

        for( var i = 0; i < path.Length; i++ )
        {
            var name = brush(path[i], p.name);

            if( i == path.Length - 1 )
            {
                var fld = p.fields.FirstOrDefault(f => f.name == name);
                if( fld != null ) return fld;

                var foundPack = p.children.FirstOrDefault(c => c.name == name);
                if( foundPack == null ) return null;
                if( !string.IsNullOrEmpty(foundPack.Reference) )
                    return get(foundPack.Reference, visited);
                return foundPack;
            }

            var next = p.children.FirstOrDefault(c => c.name == name);
            if( next == null ) return null;
            p = next;
        }

        return p;
    }

    public static string brush(string name, string class_name)
    {
        name = name.Trim()
                   .Replace('.', 'ˍ')
                   .Replace("[", "").Replace("]", "");

        // Any remaining character that is not a valid C# identifier character → '_'
        // (real-world specs use ':', '@', '$', '+', spaces etc. in names — e.g. OAuth scopes "read:pets").
        var chars = new StringBuilder(name.Length);
        foreach( var c in name ) chars.Append(char.IsLetterOrDigit(c) || c == '_' || c == 'ˍ' ? c : '_');
        name = chars.ToString();

        if( name.Length == 0 ) name = "_";
        if( char.IsDigit(name[0]) ) name = (char)(name[0] + 17) + name;

        if( name != class_name &&
            (name.Equals("_DefaultMaxLengthOf") || !HasDocs.is_prohibited(name)) )
            return name;

        return HasDocs.brush(name, class_name);
    }

    private static string GetReferencePath(BaseOpenApiReference reference)
    {
        string typeSegment;
        switch( reference.Type )
        {
            case ReferenceType.RequestBody:    typeSegment = "requestBodies"; break;
            case ReferenceType.SecurityScheme: typeSegment = "securitySchemes"; break;
            default:                           typeSegment = reference.Type.ToString().ToLowerInvariant() + "s"; break;
        }

        return $"#/components/{typeSegment}/{reference.Id}";
    }
}