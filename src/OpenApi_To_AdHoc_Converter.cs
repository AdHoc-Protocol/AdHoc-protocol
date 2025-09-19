using System;
using System.IO;
using System.Text;
using Microsoft.OpenApi.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Reader;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using org.unirail;

/**
 * @class OpenApi_To_AdHoc_Converter
 * @brief Converts OpenAPI (Swagger) specification files (JSON or YAML) into AdHoc protocol definition files.
 *
 * This class processes OpenAPI documents to generate AdHoc protocol definitions. It parses various OpenAPI components like security schemes,
 * servers, responses, parameters, schemas, paths, and operations, translating them into AdHoc protocol
 * constructs (Packs, Fields, Attributes, etc.).
 *
 * @note See https://swagger.io/docs/specification/v3_0/data-models/data-types/ for OpenAPI data type specifications.
 */
class OpenApi_To_AdHoc_Converter
{
    static int uid = 0;

    /**
     * @brief Asynchronously converts an OpenAPI specification file to an AdHoc protocol definition file.
     *
     * @param src_file The path to the source OpenAPI specification file (JSON or YAML).
     * @param dst_file The path to the destination AdHoc protocol definition file.
     * @return A Task representing the asynchronous operation.
     */
    public static async Task convert(string src_file, string dst_file)
    {
        stages.Add(new Stage());
        using var stream = new StreamReader(File.OpenRead(src_file));
        var settings = new OpenApiReaderSettings(); // Optional settings
        // Read and parse the OpenAPI document from the source file, handling both JSON and YAML formats.
        var openAPI = (await (src_file.EndsWith(".json") ?
                                  (IOpenApiReader)new OpenApiJsonReader() :
                                  new OpenApiYamlReader()).ReadAsync(stream)).OpenApiDocument;


        // Process Security Schemes defined in OpenAPI Components.
        // https://swagger.io/docs/specification/v3_0/reference/security-scheme/
        foreach (var (name, scheme) in openAPI.Components.SecuritySchemes)
        {
            var schemePack = Pack.get_or_new($"components/security/securitySchemes/{name}");
            schemePack.comment = scheme.Description; // Add description as comment to AdHoc pack
            switch (scheme.Type)
            {
                case SecuritySchemeType.OAuth2:
                    {
                        // Handle OAuth2 security schemes based on different flows.
                        if (scheme.Flows.AuthorizationCode != null)
                        {
                            // Convert Authorization Code flow details to AdHoc attributes.
                            schemePack.attributes += add_attribute("OAuth2AuthAuthorizationCode", ["AuthorizationUrl", "TokenUrl", "RefreshUrl"], [$"\"{scheme.Flows.AuthorizationCode.AuthorizationUrl}\"", $"\"{scheme.Flows.AuthorizationCode.TokenUrl}\"", $"\"{scheme.Flows.AuthorizationCode.RefreshUrl}\""]);
                            // Add scopes as Scope attributes.
                            foreach (var (Name, Description) in scheme.Flows.AuthorizationCode.Scopes) { schemePack.attributes += add_attribute("Scope", ["Name", "Description"], [$"\"{Name}\"", $"\"{Description}\""], true); }
                        }

                        if (scheme.Flows.Implicit != null)
                        {
                            // Convert Implicit flow details to AdHoc attributes.
                            schemePack.attributes += add_attribute("OAuth2AuthImplicit", ["AuthorizationUrl", "TokenUrl", "RefreshUrl"], [$"\"{scheme.Flows.Implicit.AuthorizationUrl}\"", $"\"{scheme.Flows.Implicit.TokenUrl}\"", $"\"{scheme.Flows.Implicit.RefreshUrl}\""]);
                            // Add scopes as Scope attributes.
                            foreach (var (Name, Description) in scheme.Flows.Implicit.Scopes) { schemePack.attributes += add_attribute("Scope", ["Name", "Description"], [$"\"{Name}\"", $"\"{Description}\""], true); }
                        }


                        if (scheme.Flows.Password != null)
                        {
                            // Convert Password flow details to AdHoc attributes.
                            schemePack.attributes += add_attribute("OAuth2AuthPassword", ["TokenUrl", "RefreshUrl"], [$"\"{scheme.Flows.Password.TokenUrl}\"", $"\"{scheme.Flows.Password.RefreshUrl}\""]);
                            // Add scopes as Scope attributes.
                            foreach (var (Name, Description) in scheme.Flows.Password.Scopes) { schemePack.attributes += add_attribute("Scope", ["Name", "Description"], [$"\"{Name}\"", $"\"{Description}\""], true); }
                        }

                        if (scheme.Flows.ClientCredentials != null)
                        {
                            // Convert Client Credentials flow details to AdHoc attributes.
                            schemePack.attributes += add_attribute("OAuth2AuthClientCredentials", ["TokenUrl", "RefreshUrl"], [$"\"{scheme.Flows.ClientCredentials.TokenUrl}\"", $"\"{scheme.Flows.ClientCredentials.RefreshUrl}\""]);
                            // Add scopes as Scope attributes.
                            foreach (var (Name, Description) in scheme.Flows.ClientCredentials.Scopes) { schemePack.attributes += add_attribute("Scope", ["Name", "Description"], [$"\"{Name}\"", $"\"{Description}\""], true); }
                        }

                        break;
                    }
                case SecuritySchemeType.ApiKey:
                    // Convert ApiKey security scheme to ApiKeyAuth AdHoc attribute.
                    schemePack.attributes += add_attribute("ApiKeyAuth", ["Name", "In"], [$"\"{scheme.Name}\"", $"\"{scheme.In}\""]);
                    break;
                case SecuritySchemeType.Http:
                    // Convert HTTP security scheme (Basic or Bearer) to AdHoc attributes.
                    if (scheme.Scheme.Equals("bearer", StringComparison.OrdinalIgnoreCase))
                        schemePack.attributes += add_attribute("BearerAuth", ["BearerFormat"], [$"\"{scheme.BearerFormat}\""]);
                    else
                        schemePack.attributes += add_attribute("BasicAuth", [], []); // BasicAuth has no specific parameters in AdHoc

                    break;
                case SecuritySchemeType.OpenIdConnect:
                    // Convert OpenIdConnect security scheme to OpenIdConnect AdHoc attribute.
                    schemePack.attributes += add_attribute("OpenIdConnect", ["OpenIdConnectUrl"], [$"\"{scheme.OpenIdConnectUrl}\""]);
                    break;
            }
        }

        // Process Host Information from OpenAPI 'servers' to AdHoc 'host'.
        // https://swagger.io/docs/specification/v3_0/ Describing Servers
        read_servers(openAPI.Servers);

        // --- Response Processing ---
        // Responses are used in Operations and Components.
        // In Operations: Defines responses for each API operation (e.g., GET, POST).
        // In Components: Defines reusable responses under components.responses for referencing in operations.
        //
        // @brief Creates AdHoc packs for OpenAPI Responses and their content.
        //
        // @param path The base path for creating AdHoc packs (e.g., "components/responses", "paths/{path}/{operation}").
        // @param Responses A dictionary of OpenAPI responses (status code or name to OpenApiResponse).
        // @param server_branch Optional branch to add the created packs to. Used for operation-specific responses.
        //
        void create_response_packs(string path, IDictionary<string, OpenApiResponse> Responses, Branch? server_branch)
        {
            if (server_branch == null)
                // Process Responses defined in Components (reusable responses).
                foreach (var (_200_404_500, response) in Responses) // Responses for each operation
                {
                    var pack_ = Pack.get_or_new($"{path}/{(char.IsDigit(_200_404_500[0]) ? $"Code_{200_404_500}" : _200_404_500)}");
                    pack_.add_comment(response.Description!); // Add response description as comment

                    // Process content types and schemas for each response.
                    foreach (var (contentType, mediaType) in response.Content!)
                        if (mediaType.Schema?.Reference == null)
                            // Inline schema definition - create a Field directly in the pack.
                            new Field(pack_, contentType, mediaType.Schema);
                        else
                            // Schema reference - add inheritance to the pack.
                            pack_.add_inherits(mediaType.Schema.Reference.ReferenceV3);

                    // Process response links.
                    foreach (var link in response.Links) pack_.attributes += add_attribute("Link", ["Key", "OperationId"], [$"\"{link.Key}\"", $"\"{link.Value.OperationId}\""]);

                    // Process headers defined in the response.
                    if (0 < response.Headers?.Count)
                        foreach (var (headerName, header) in response.Headers)
                            new Field(pack_, headerName, header.Schema)
                                .add_comment(header.Description) // Add header description as comment.
                                .add_attributes(add_attribute("ResponseHeader", [], [])); // Mark header as ResponseHeader attribute.
                }
            else
            {
                // Process Responses defined in Operations (operation-specific responses).
                // Logic for merging similar response schemas to reduce code duplication.

                var inherits = new Dictionary<string, string>(); // Track schema inheritance for merging.
                var info = new int[Responses.Count];         // 0: new, 1: new merged, 2: skip (merged already)

                var i = -1;
                foreach (var (_200_404_500, response) in Responses) // Iterate through operation responses.
                {
                    i++;
                    var same_inherits = "";

                    // Check if responses can be merged based on schema inheritance and other properties.
                    if (response.Links.Count == 0 && !(0 < response.Headers?.Count)) // Merging criteria: no links and no headers.
                        foreach (var (contentType, mediaType) in response.Content)
                            if (mediaType.Schema?.Reference == null || same_inherits != "" && same_inherits != mediaType.Schema.Reference.ReferenceV3)
                                break; // Do not merge, create new pack.
                            else if (inherits.TryGetValue(same_inherits = mediaType.Schema.Reference.ReferenceV3, out var value))
                            {
                                if (_200_404_500 == value) continue; // Skip if same status code, different media type (already processed).
                                inherits[mediaType.Schema.Reference.ReferenceV3] = value + "_" + _200_404_500; // Merge status codes.
                                info[i] = 2; // Mark as merged, skip pack creation later.
                            }
                            else
                            {
                                inherits[mediaType.Schema.Reference.ReferenceV3] = _200_404_500; // Start new merge group.
                                info[i] = 1; // Mark as new merged pack creation.
                            }
                }

                i = -1;
                foreach (var (_200_404_500, response) in Responses) // Create packs based on merging info.
                {
                    i++;
                    if (info[i] == 2) continue; // Skip if already merged.
                    var pack_ = Pack.get_or_new($"{path}/Code_{(info[i] == 1 ? inherits[response.Content.Values.First().Schema!.Reference.ReferenceV3] : _200_404_500)}");
                    pack_.add_comment(response.Description); // Add response description as comment.

                    server_branch?.packs.Add(pack_); // Add pack to server branch.
                    if (info[i] == 1)
                    {
                        // Merged pack - inherit schema.
                        pack_.add_inherits(response.Content.Values.First().Schema!.Reference.ReferenceV3);
                        continue; // Skip further processing for merged packs.
                    }

                    // Process content types and schemas for non-merged responses.
                    foreach (var (contentType, mediaType) in response.Content)
                        if (mediaType.Schema?.Reference == null)
                            // Inline schema definition - create a Field directly in the pack.
                            new Field(pack_, contentType, mediaType.Schema);
                        else
                            // Schema reference - add inheritance to the pack.
                            pack_.add_inherits(mediaType.Schema.Reference.ReferenceV3);

                    // Process response links.
                    foreach (var link in response.Links) pack_.attributes += add_attribute("Link", ["Key", "OperationId"], [$"\"{link.Key}\"", $"\"{link.Value.OperationId}\""]);

                    // Process headers defined in the response.
                    if (0 < response.Headers?.Count)
                        foreach (var (headerName, header) in response.Headers)
                            new Field(pack_, headerName, header.Schema)
                                .add_comment(header.Description) // Add header description as comment.
                                .add_attributes(add_attribute("ResponseHeader", [], [])); // Mark header as ResponseHeader attribute.
                }
            }
        }


        // --- Process Components - Responses ---
        // https://swagger.io/docs/specification/v3_0/describing-responses/
        if (0 < openAPI.Components?.Responses?.Count)
            create_response_packs("components/responses", openAPI.Components.Responses, null);


        // --- Process Components - Parameters ---
        // https://swagger.io/docs/specification/v3_0/describing-parameters/
        if (0 < openAPI.Components?.Parameters?.Count)
        {
            var components_parameters = Pack.get_or_new("components/parameters");

            foreach (var (name, parameter) in openAPI.Components.Parameters)
            {
                // Create a Field for each parameter in components/parameters.
                var fld = new Field(components_parameters, name, parameter.Schema) // schema: Defines the type and format of the parameter.
                {
                    optional = !parameter.Required                                     // Boolean indicating if parameter is mandatory.
                }.add_attributes(add_attribute("In", ["In"], [$"\"{parameter.In}\""])) // in: Location (path, query, header, cookie).
                     .add_comment(parameter.Description);                                // Add parameter description as comment.

                if (parameter.Deprecated)
                    fld.add_attributes(add_attribute("Deprecated", [], [])); // Add Deprecated attribute if parameter is deprecated.
            }
        }

        // --- Process Components - Schemas ---
        // https://swagger.io/docs/specification/v3_0/data-models/data-models/
        if (0 < openAPI.Components?.Schemas?.Count)
        {
            var components_schemas = Pack.get_or_new("components/schemas");

            foreach (var (name, schema) in openAPI.Components!.Schemas!)
            {
                if (schema.Reference != null) // Self reference handling.
                {
                    var p = Pack.get_or_new("components/schemas/" + name);
                    p.add_comment(schema.Description);
                    p.Reference = schema.Reference.ReferenceV3; // Store reference path.
                    continue;
                }

                if (schema.AdditionalProperties != null) // https://swagger.io/docs/specification/v3_0/data-models/dictionaries/
                {
                    // Handle dictionaries (maps) using AdditionalProperties.
                    new Field(components_schemas, name, schema.AdditionalProperties, true).add_comment(schema.Description);
                    continue;
                }

                // Handle simple schema types without properties, allOf, oneOf, anyOf.
                if (schema.Type != null && schema.Properties.Count == 0 && schema.AllOf.Count == 0 && schema.OneOf.Count == 0 && schema.AnyOf.Count == 0)
                {
                    new Field(components_schemas, name, schema).add_comment(schema.Description);
                    continue;
                }


                var pack = Pack.get_or_new("components/schemas/" + name); // Pack for the schema (will be referenced by other parts).
                pack.comment = schema.Description; // Add schema description as comment.
                add_fields(schema, pack); // Process schema properties.

                // Adds fields to a Pack based on the properties of an OpenApiSchema.
                //
                // @param src The source OpenApiSchema containing properties.
                // @param dst The destination Pack to which fields will be added.
                //
                void add_fields(OpenApiSchema? src, Pack dst)
                {
                    foreach (var (fld_name, fld_schema) in src.Properties)
                        new Field(dst, fld_name, fld_schema)
                        {
                            optional = !src.Required.Contains(fld_name) || fld_schema.Nullable // Field is optional if not in required list or is nullable.
                        };
                }

                // Process 'allOf' composition (inheritance).
                foreach (var AllOf in schema.AllOf)
                    if (AllOf.Reference == null) // Inline 'allOf' object - add fields directly.
                        add_fields(AllOf, pack);
                    else
                        pack.add_inherits(AllOf.Reference.ReferenceV3); // Schema reference - add inheritance.

                var inline = 0;
                // Process 'oneOf' composition (polymorphism).
                foreach (var OneOf in schema.OneOf)
                    if (OneOf.Reference == null) // Inline 'oneOf' object - create inline pack and field.
                    {
                        var dst = Pack.get_or_new("components/schemas/" + name + "/Inline" + inline++);
                        add_fields(OneOf, dst); // Add fields to inline pack.

                        new Field(pack, "OneOf_" + dst.name, dst.name) // Add field referencing the inline pack.
                        {
                            optional = true // 'oneOf' fields are optional.
                        };
                    }
                    else
                        new Field(pack, "OneOf_" + OneOf.Reference.ReferenceV3.Split('/')[^1], OneOf) // Schema reference - add field referencing the schema.
                        {
                            optional = true // 'oneOf' fields are optional.
                        };


                // Process 'anyOf' composition (polymorphism).
                foreach (var AnyOf in schema.AnyOf)
                    if (AnyOf.Reference == null) // Inline 'anyOf' object - create inline pack and field.
                    {
                        var dst = Pack.get_or_new("components/schemas/" + name + "/Inline" + inline++);
                        add_fields(AnyOf, dst); // Add fields to inline pack.

                        new Field(pack, "AnyOf_" + dst.name, dst.name) // Add field referencing the inline pack.
                        {
                            optional = true // 'anyOf' fields are optional.
                        };
                    }
                    else
                        new Field(pack, "AnyOf_" + AnyOf.Reference.ReferenceV3.Split('/')[^1], AnyOf) // Schema reference - add field referencing the schema.
                        {
                            optional = true // 'anyOf' fields are optional.
                        };


                // Process schema extensions (vendor extensions).
                // https://swagger.io/docs/specification/v3_0/openapi-extensions/?sbsearch=Extensions
                foreach (var (extKey, extValue) in schema.Extensions)
                    pack.attributes += add_attribute("Extension", ["ExtKey", "ExtValue"], [$"\"{extKey}\"", $"\"{extValue}\""], true);


                // Discriminator - Not used in AdHoc protocol directly.
                // Allows API consumers to detect object type. Handled implicitly by optional 'oneOf', 'anyOf' fields.
                // if( schema.Discriminator != null ) // skip it
                //{
                //Not used in the AdHoc protocol description. Check fields for null to find what type passed
                //}

                // XML attributes - Not supported in AdHoc protocol.
                // Allows customization of XML representation of API data.
                // if( schema.Xml != null ) // skip it
                // { }
            }
        }

        // --- Process Paths and Operations ---
        // https://swagger.io/docs/specification/v3_0/paths-and-operations/
        foreach (var (path, item) in openAPI.Paths)                       // Iterate through API paths.
            foreach (var (get_post_delete, operation) in item.Operations) // Iterate through operations (get, post, put, patch, delete, head, options).
            {
                var pack = Pack.get_or_new($"paths/{clean_path(path)}/{get_post_delete}"); // Create Pack for each operation, using cleaned path and operation type.

                var client_branch = new Branch(); // Create a client branch for this operation.
                client_branch.packs =
                [
                    pack
                ];

                pack.comment = operation.Description; // Add operation description as comment.
                if (!string.IsNullOrEmpty(operation.OperationId)) pack.attributes += add_attribute("OperationId", ["OperationId"], [$"\"{operation.OperationId}\""]); // Add OperationId attribute.
                if (!string.IsNullOrEmpty(operation.Summary)) pack.attributes += add_attribute("Summary", ["Summary"], [$"\"{operation.Summary}\""]);         // Add Summary attribute.
                if (operation.Deprecated) pack.attributes += add_attribute("Obsolete", ["Message"], [$"\"This operation is deprecated\""]); // Add Obsolete attribute for deprecated operations.
                foreach (var tag in operation.Tags) pack.attributes += add_attribute("Tag", ["Name", "Description"], [$"\"{tag.Name}\"", $"\"{tag.Description}\""], true); // Add Tag attributes for operation tags.

                // Add support for operation extensions (vendor extensions).
                if (operation.Extensions?.Count > 0)
                    foreach (var (ext_name, ext_Value) in operation.Extensions)
                        pack.attributes += add_attribute(brush(ext_name, ""), ["Value"], [$"\"{ext_Value}\""]); // Add Extension attributes for operation extensions.

                // Process operation parameters.
                foreach (var parameter in operation.Parameters) // Iterate through operation parameters.
                {
                    var fld = new Field(pack, parameter.Name, parameter.Schema) // schema: Defines the type and format of the parameter.
                    {
                        optional = !parameter.Required                                     // Boolean indicating if parameter is mandatory.
                    }.add_attributes(add_attribute("In", ["In"], [$"\"{parameter.In}\""])) // in: Location (path, query, header, cookie).
                         .add_comment(parameter.Description);                                // Add parameter description as comment.

                    if (parameter.Deprecated)
                        fld.add_attributes(add_attribute("Deprecated", [], [])); // Add Deprecated attribute if parameter is deprecated.
                }

                // Process Request Body of the operation.
                if (operation.RequestBody != null)
                    foreach (var (contentType, mediaType) in operation.RequestBody.Content)
                    {
                        new Field(pack, $"Request_{contentType}", mediaType.Schema) // Create field for request body content.
                        {
                            optional = !operation.RequestBody.Required, // Request body is optional if not required.
                        }.add_comment(operation.RequestBody.Description); // Add request body description as comment.
                    }

                // Process Security Requirements for the operation.
                if (0 < operation.Security.Count)
                    foreach (var requirement in operation.Security)
                        foreach (var (schemeName, scopes) in requirement)
                            pack.attributes += add_attribute("SecurityRequirement",      // Add SecurityRequirement attribute.
                                                             ["SchemeName", "Scopes"],    // Attribute arguments: Scheme name and scopes.
                                                             [$"\"{schemeName}\"", $"\"{string.Join(",", scopes)}\""]); // Attribute values.
                var server_branch = new Branch(); // Create a server branch for operation responses.
                server_branch.packs = [];

                if (0 < operation.Responses.Count)
                    create_response_packs($"paths/{clean_path(path)}/{get_post_delete}", operation.Responses, server_branch); // Process operation responses.

                // --- Process Callbacks ---
                // https://swagger.io/docs/specification/v3_0/callbacks/
                if (0 < operation.Callbacks.Count)
                    foreach (var (name, Callback) in operation.Callbacks)
                    {
                        var callbackPack = Pack.get_or_new($"{pack.name}/callbacks/{name}"); // Create Pack for callback.

                        foreach (var (callbackPath, callbackItem) in Callback.PathItems)
                        {
                            foreach (var (callbackOperationType, callbackOperation) in callbackItem.Operations)
                            {
                                var cbOpPack = Pack.get_or_new($"{callbackPack.name}/{callbackOperationType}"); // Create Pack for callback operation.
                                cbOpPack.comment = callbackOperation.Description; // Add callback operation description as comment.
                                cbOpPack.attributes += add_attribute("OperationId", ["OperationId"], [$"\"{callbackOperation.OperationId}\""]); // Add OperationId attribute.
                                cbOpPack.attributes += add_attribute("Summary", ["Summary"], [$"\"{callbackOperation.Summary}\""]);     // Add Summary attribute.
                                foreach (var tag in callbackOperation.Tags) cbOpPack.attributes += add_attribute("Tag", ["Name", "Description"], [$"\"{tag.Name}\"", $"\"{tag.Description}\""], true); // Add Tag attributes.

                                // Process request body schema for callbacks (inheritance only).
                                if (callbackOperation.RequestBody != null)
                                    foreach (var (key, application_json) in callbackOperation.RequestBody.Content)
                                        if (application_json.Schema != null && !string.IsNullOrEmpty(application_json.Schema.Reference.ReferenceV3))
                                            cbOpPack.add_inherits(application_json.Schema.Reference.ReferenceV3); // Add inheritance for callback request schema.
                            }
                        }
                    }

                add_stage(client_branch, server_branch); // Add client and server branches to stages.
            }

        var sb = new StringBuilder();
        root.write(sb); // Generate AdHoc protocol definition string.
        File.WriteAllText(dst_file, @$"
using System;
using org.unirail.Meta;

namespace com.my.company // Your company namespace. Required!
{{
    {(string.IsNullOrEmpty(openAPI.Info.Title) ? "" : add_attribute("Title", ["Title"], [$"\"{openAPI.Info.Title}\""]))}
    {(string.IsNullOrEmpty(openAPI.Info?.Version) ? "" : add_attribute("Version", ["Version"], [$"\"{openAPI.Info.Version}\""]))}
    {(string.IsNullOrEmpty(openAPI.Info?.Description) ? "" : add_attribute("Description", ["Description"], [$"\"{openAPI.Info.Description}\""]))}
    {(openAPI.Info?.Contact == null ? "" : add_attribute("Contact", ["Name", "Email", "Url"], [$"\"{openAPI.Info.Contact?.Name}\"", $"\"{openAPI.Info.Contact?.Email}\"", $"\"{openAPI.Info.Contact?.Url}\""]))}
    {(string.IsNullOrEmpty(openAPI.Info?.License?.Name) ? "" : add_attribute("License", ["Name", "Url"], [$"\"{openAPI.Info.License?.Name ?? "N/A"}\"", $"\"{openAPI.Info.License?.Url}\""]))}

    public interface {Path.GetFileNameWithoutExtension(src_file)}
    {{

    {sb}
    {hosts}
    ///<see cref = 'InTS'/>   implementation in TypeScript
    ///<see cref = 'InCS'/>   implementation in C#
    ///<see cref = 'InJAVA'/> implementation in JAVA
    ///<see cref = 'InCPP'/>  implementation in C++
    ///<see cref = 'InRS'/>   implementation in RUST
    ///<see cref = 'InGO'/>   implementation in GO
    struct Client:Host{{}}

    interface ClientServerChannel:ChannelFor<Client,Server0>{{
        interface Start:L,
                       {string.Join(",\n", stages[0].client_branches.Select(br => $"_<\n{string.Join(",\n", br.packs)},\n Stage{br.dstStage}\n>"))}{{}}
        {string.Join("\n", stages.Skip(1).Select((st, i) => $"interface Stage{i + 1}:R,\n{string.Join(",\n", st.server_branches.Select(br => $"_<\n{string.Join(",\n", br.packs)},\n Start\n>"))}{{ }}"))}
    }}


    {string.Join('\n', attributes.Values)}

    }}
}}
"); // Write the generated AdHoc protocol definition to the destination file.
    }

    // --- Attribute Definition and Helper Methods ---
    /**
     * @brief Dictionary to store defined attributes and their C# class definitions.
     */
    public static Dictionary<string, string> attributes = new()
                                                          {
                                                              { "Date", $"public class DateAttribute : Attribute{{ public DateAttribute(){{}}   public DateAttribute(string from_date, string to_date = \"\", long precision_days=1) {{ }}}}" },
                                                              { "DateTime", $@"public class DateTimeAttribute : Attribute{{  public DateTimeAttribute(){{}}   public DateTimeAttribute(string from_date_time, string to_date_time = """", long precision_msec=1) {{ }}}}" },
                                                              { "Time", $@"public class TimeAttribute : Attribute{{ public TimeAttribute(){{}}   public TimeAttribute(string from_time, string to_time = """", long precision_msec=1) {{ }}}}" }
                                                          };

    /**
     * @brief Adds an attribute definition to the attributes dictionary and returns its C# usage string.
     *
     * @param name The name of the attribute.
     * @param args_name An optional array of argument names for the attribute constructor.
     * @param args_values An array of argument values for the attribute constructor.
     * @param AllowMultiple Indicates if the attribute can be applied multiple times (default: false).
     * @return The C# string representing the attribute usage (e.g., "[AttributeName(arg1, arg2)]").
     */
    public static string add_attribute(string name, string[]? args_name, string[] args_values, bool AllowMultiple = false)
    {
        if (!attributes.ContainsKey(name))
            attributes[name] = $@" {(AllowMultiple ? "[AttributeUsage(AttributeTargets.All , AllowMultiple = true)]\n" : "")} public class {name}Attribute : Attribute{{ public {name}Attribute( {string.Join(',', args_values.Select((a, i) => $"{(a[0] == '\"' ? "string" : "long")} {(args_name == null ? $"arg{i}" : args_name[i])}"))} ) {{ }} }} ";

        return args_values.All(a => a == "\"\"" || a == "") ?
                   $"[{name}]" :
                   $"[{name}( {string.Join(',', args_values.Select(a => a[0] == '\"' ? "@" + a : a))} )]\n";
    }


    // --- Entity Class (Base class for Packs and Fields) ---
    /**
     * @class Entity
     * @brief Base class for representing AdHoc protocol entities (Packs and Fields).
     *
     * Provides common properties and methods for Packs and Fields, including parent-child relationships,
     * name handling, comment storage, attribute storage, and writing to string builder.
     */
    public abstract class Entity
    {
        public Pack? parent;   // Parent Pack of this entity. Null for root Pack.
        public string name;      // Name of the entity.

        /**
         * @brief Abstract method to write the entity's definition to a StringBuilder.
         * @param dst The StringBuilder to write to.
         */
        public abstract void write(StringBuilder dst);

        /**
         * @brief Returns the fully qualified name of the entity, including parent names.
         * @return Fully qualified name string.
         */
        public override string ToString()
        {
            void scan(Entity src, StringBuilder dst)
            {
                if (src.parent == null || src.parent == root) dst.Append(src.name); // Base case: root or no parent.
                else
                {
                    scan(src.parent!, dst); // Recursively scan parent.
                    dst.Append('.').Append(src.name); // Append current name.
                }
            }

            scan(this, tmp.Clear()); // Start recursive scan.
            return tmp.ToString();
        }

        public string comment; // Comment associated with the entity.

        /**
         * @brief Adds a comment to the entity's comment string.
         * @param comment The comment string to add.
         * @return The entity instance for chaining.
         */
        public Entity add_comment(string comment)
        {
            this.comment += comment + "\n";
            return this;
        }

        public string attributes; // Attributes string associated with the entity.

        /**
         * @brief Adds attributes string to the entity's attributes string.
         * @param attributes The attributes string to add.
         * @return The entity instance for chaining.
         */
        public Entity add_attributes(string attributes)
        {
            this.attributes += attributes + "\n";
            return this;
        }
    }

    /**
     * @brief Helper method to format comments for AdHoc protocol definitions.
     * @param comment The comment string.
     * @return Formatted comment string enclosed in block comments, or empty string if comment is null or empty.
     */
    static string _comment(string? comment) => string.IsNullOrEmpty(comment?.Trim()) ?
                                                   "" :
                                                   $@"
/** {comment} */
";


    // --- Field Class (Represents a field in an AdHoc Pack) ---
    /**
     * @class Field
     * @brief Represents a field within an AdHoc Pack.
     *
     * Holds information about a field's name, type, schema, optionality, and associated attributes and comments.
     */
    public class Field : Entity
    {
        public OpenApiSchema? Schema;   // OpenAPI schema associated with the field. Null for enum fields.
        public bool optional; // Indicates if the field is optional.
        public bool has_Set_type; // Indicates if the field is a Set type.
        public bool has_Map_type; // Indicates if the field is a Map type.
        public string inline_type = ""; // For inline types (e.g., enum, nested objects).

        /**
         * @brief Constructor for creating a Field with an inline type (e.g., enum reference).
         * @param pack The parent Pack.
         * @param name The name of the field.
         * @param inline_type The inline type name.
         */
        public Field(Pack pack, string name, string inline_type)
        {
            parent = pack;
            this.name = brush(name, pack.name); // Brush field name to ensure valid AdHoc name.
            this.inline_type = inline_type;
            pack.fields.Add(this);
        }

        /**
         * @brief Constructor for creating a Field with an OpenAPI schema definition.
         * @param pack The parent Pack.
         * @param name The name of the field.
         * @param schema The OpenAPI schema for the field.
         * @param has_Map_type Indicates if the field is a Map type (default: false).
         */
        public Field(Pack pack, string name, OpenApiSchema? schema, bool has_Map_type = false)
        {
            parent = pack;
            this.name = brush(name, pack.name); // Brush field name to ensure valid AdHoc name.
            Schema = schema;

            if (schema != null) // Process schema properties if schema is not null (not enum field).
            {
                has_Set_type = schema.UniqueItems ?? false; // Check for UniqueItems for Set type.
                this.has_Map_type = has_Map_type; // Indicate if it's a Map type.

                if (schema.MaxLength.HasValue) // Apply MaxLength attribute for string fields.
                    attributes += $"[D( +{schema.MaxLength})]\n";

                var Default = "0"; // Default value for MinMax attribute.

                if (schema.Default != null) // Process default value from schema.
                {
                    if (!OpenApi_To_AdHoc_Converter.attributes.ContainsKey("Default"))
                        OpenApi_To_AdHoc_Converter.attributes["Default"] = "public class DefaultAttribute : Attribute{ public DefaultAttribute(double value){} public DefaultAttribute(long value){} public DefaultAttribute(string value){} }";

                    switch (schema.Default) // Handle different default value types.
                    {
                        case System.Text.Json.Nodes.JsonValue val when val.TryGetValue(out long longVal):
                            attributes += $"[Default({Default = longVal.ToString()})]\n";
                            break;
                        case System.Text.Json.Nodes.JsonValue val when val.TryGetValue(out double doubleVal):
                            attributes += $"[Default({Default = doubleVal.ToString()})]\n";
                            break;
                    }
                }

                if (schema.Minimum.HasValue || schema.Maximum.HasValue) // Apply MinMax attribute for numeric fields.
                    attributes += $"[MinMax({(schema.Minimum.HasValue ? schema.Minimum : Default)}, {(schema.Maximum.HasValue ? schema.Maximum : Default)})]\n";

                if (!string.IsNullOrEmpty(schema.Pattern)) // Apply Pattern attribute for string fields.
                    attributes += add_attribute("Pattern", ["Pattern"], ['\"' + schema.Pattern + '\"']);

                if (schema.ReadOnly) // Apply ReadOnly attribute.
                    attributes += add_attribute("ReadOnly", [], []);

                if (schema.WriteOnly) // Apply WriteOnly attribute.
                    attributes += add_attribute("WriteOnly", [], []);

                if (schema.MultipleOf != null) // Apply MultipleOf attribute for numeric fields.
                    attributes += add_attribute("MultipleOf", ["MultipleOf"], [schema.MultipleOf.ToString()!]);

                if (schema.MinItems != null) // Apply MinItems attribute for array/set/map fields.
                    attributes += add_attribute("MinItems", ["MinItems"], [schema.MinItems.ToString()!]);

                if (schema.MaxItems != null) // Apply MaxItems attribute (using D attribute for variable length array).
                    attributes += $"[D({(has_Set_type || has_Map_type ? "+" : "")}{schema.MaxItems})]\n";

                if (0 < schema.Enum?.Count) // Handle enum definitions within schema.
                {
                    var en = Pack.get_enum(pack.ToString().Replace('.', '/') + "/enum_for_" + this.name, schema.Enum); // Create enum Pack.
                    en.comment = schema.Description; // Add enum description as comment.
                    inline_type = en.ToString(); // Set inline type to enum Pack name.
                }
            }

            pack.fields.Add(this); // Add field to the parent Pack.
        }

        public string value = ""; // For enum fields, stores the enum value.

        /**
         * @brief Determines the AdHoc data type string for the field based on its OpenAPI schema.
         * @param schema The OpenAPI schema.
         * @return The AdHoc data type string.
         */
        string type(OpenApiSchema schema)
        {
            if (inline_type != "") return inline_type; // Return inline type if defined (e.g., enum).

            if (schema.Reference != null) // Handle schema references.
                switch (get(schema.Reference.ReferenceV3)) // Resolve reference path.
                {
                    case Field fld:
                        return fld.type(fld.Schema!); // Recursively get type from referenced field.
                    case Pack pk:
                        return pk!.ToString()!;      // Return Pack name for schema reference.
                    case null:
                        Console.Out.WriteLine($"ERROR: Unknown type: {schema.Reference.ReferenceV3}");
                        break;
                }

            switch (schema.Type) // Map OpenAPI schema types to AdHoc types.
            {
                case JsonSchemaType.Integer:

                    return schema.Format == "int64" ?
                               "long" :
                               "int";
                case JsonSchemaType.Number:

                    return schema.Format == "float" ?
                               "float" :
                               "double";
                case JsonSchemaType.String:

                    if (!string.IsNullOrEmpty(schema.Format)) // Apply format-specific attributes for string types.
                        attributes += schema.Format switch
                        {
                            "binary" => "",                                                                                          // binary file contents
                            "byte" => add_attribute("Base64", null, []) + '\n', // base64-encoded file contents
                            "date" => add_attribute("Date", null, []) + '\n',
                            "date-time" => add_attribute("DateTime", null, []) + '\n',
                            "password" => add_attribute("Password", null, []) + '\n',
                            _ => add_attribute(char.ToUpper(schema.Format[0]) + schema.Format.Substring(1), null, []) + '\n'
                        };
                    return schema.Format == "binary" ?
                               "Bynary" :
                               "string";
                case JsonSchemaType.Boolean: return "bool";
                case JsonSchemaType.Array:

                    return $"{type(schema.Items)}[{(schema.Maximum.HasValue ? ",," : "")}]"; // Variable length array if MaxLength is defined.
                case JsonSchemaType.Object:

                    return schema.AdditionalProperties == null ?
                               "Binary" : // Represent generic Object as Binary type.
                               $"Map< string , {type(schema.AdditionalProperties)}>"; // Map type if AdditionalProperties is defined.
                default:
                    return "Binary"; // Default to Binary for unknown types.
            }
        }

        /**
         * @brief Writes the Field's AdHoc definition to a StringBuilder.
         * @param dst The StringBuilder to write to.
         */
        public override void write(StringBuilder dst)
        {
            if (Schema == null) // Handle enum fields.
            {
                dst.Append(name);
                if (value != "") dst.Append(" = ").Append(value); // Append enum value if defined.
                dst.Append(',').AppendLine();
                return;
            }

            var V = "";
            var T = type(Schema); // Get AdHoc data type.
            if (has_Set_type) T = $"Set<{T}>"; // Wrap type in Set if it's a Set.
            else if (has_Map_type)
                if (T.Contains("Map<")) // Handle nested Map types.
                {
                    V = $"class {name}_V {{ {T} V;   }} "; // Create inline class for nested Map value type.
                    T = $"Map< string , {name}_V>"; // Map to the inline class.
                }
                else T = $"Map< string , {T}>"; // Map type with resolved value type.

            dst.Append($"""
                            {_comment(comment + "\n" + Schema.Description)}
                            {attributes}{T} {(optional ? "?" : "")} {name};
                            {V}
                        """); // Write field definition to StringBuilder.
        }
    }

    /**
     * @brief Cleans and normalizes a path string for use in AdHoc Pack names.
     *
     * Replaces special characters like '{', '}', ':', '-', '[', ']', ' ' with '_', and trims leading/trailing '/'.
     * @param refPath The path string to clean.
     * @return The cleaned path string.
     */
    static string clean_path(string refPath) => refPath
                                                .Replace("{", "I__")
                                                .Replace("}", "__I")
                                                .Replace(":", "")
                                                .Replace('-', '_')
                                                .Replace("[", "").Replace("]", "").Replace(" ", "_")
                                                .TrimStart('#', '/')
                                                .TrimEnd('/');

    static StringBuilder tmp = new StringBuilder(); // Temporary StringBuilder for ToString() method.

    /**
     * @brief Retrieves an Entity (Pack or Field) by its reference path.
     *
     * Traverses the Pack hierarchy based on the path segments.
     * @param ref_path The reference path string (e.g., "components/schemas/MySchema").
     * @return The found Entity, or null if not found.
     */
    public static object? get(string ref_path)
    {
        var p = root; // Start from the root Pack.
        var path = clean_path(ref_path).Split('/'); // Split path into segments.

        for (var i = 0; i < path.Length; i++)
        {
            var name = brush(path[i], p.name); // Brush path segment name.

            if (i == path.Length - 1) // Last segment - look for Field or Pack.
            {
                var fld = p.fields.FirstOrDefault(fld => fld.name == name); // Check for Field with matching name.
                return fld ?? ((p = p.children.FirstOrDefault(pack => pack.name == name)) == null ? // Check for Pack with matching name.
                                   null :
                                   p.Reference == "" ? // If Pack found, check if it's a reference or definition.
                                       p :         // Return Pack definition.
                                       get(p.Reference)); // Resolve and return referenced Pack.
            }

            var next = p.children.FirstOrDefault(pack => pack.name == name); // Find child Pack with matching name.
            if (next == null) return null; // Path segment not found.
            p = next; // Move to the next Pack in the hierarchy.
        }

        return p; // Return the final Pack if path traversal successful.
    }

    // --- Pack Class (Represents a group of Fields and child Packs) ---
    /**
     * @class Pack
     * @brief Represents a group of Fields and child Packs in the AdHoc protocol definition.
     *
     * Corresponds to a class or interface in the generated C# code. Can contain Fields, child Packs,
     * inheritance information, and attributes.
     */
    public class Pack : Entity
    {
        public List<Field> fields = [];    // List of Fields within the Pack.
        public List<Pack> children = [];  // List of child Packs.
        public HashSet<string> inherits = [];  // Set of paths to inherited Packs.
        public string Reference = "";    // Path to referenced Pack (if this Pack is just a reference).

        /**
         * @brief Adds an inheritance path to the Pack.
         * @param path The path string to the Pack to inherit from.
         */
        public void add_inherits(string path) => inherits.Add(path);

        public bool is_enum; // Indicates if the Pack represents an enum.

        /**
         * @brief Registers an enum Pack at the specified path.
         *
         * If an enum Pack already exists at the given path, it is returned; otherwise, a new enum Pack is created,
         * registered, and returned.
         * @param ref_path The reference path for the enum Pack.
         * @param Enum List of JsonNode representing enum values.
         * @return The registered enum Pack.
         */
        public static Pack get_enum(string ref_path, IList<System.Text.Json.Nodes.JsonNode> Enum)
        {
            var en = get_or_new(ref_path); // Get or create Pack at the given path.
            en.is_enum = true; // Mark Pack as enum.

            foreach (var fld in Enum) // Iterate through enum values.
                switch (fld)
                {
                    case System.Text.Json.Nodes.JsonValue val when val.TryGetValue(out long longVal):
                        new Field(en, "x" + longVal, null) // Create Field for long enum value.
                        {
                            value = longVal.ToString() // Set enum value string.
                        };
                        break;
                    case System.Text.Json.Nodes.JsonValue val when val.TryGetValue(out double doubleVal):
                        new Field(en, "x" + doubleVal, null) // Create Field for double enum value.
                        {
                            value = doubleVal.ToString() // Set enum value string.
                        };
                        break;

                    case System.Text.Json.Nodes.JsonValue val when val.TryGetValue(out bool boolVal):
                        new Field(en, "x" + boolVal, null); // Create Field for boolean enum value.

                        break;
                    case System.Text.Json.Nodes.JsonValue val:
                        if (val.TryGetValue(out string? strVal))
                            new Field(en, strVal, null); // Create Field for string enum value.


                        break;
                }

            return en; // Return the enum Pack.
        }

        /**
         * @brief Registers a Pack at the specified path.
         *
         * If a Pack already exists at the given path, it is returned; otherwise, a new Pack is created,
         * registered, and returned.
         * @param ref_path The reference path for the Pack.
         * @return The registered Pack.
         */
        public static Pack get_or_new(string ref_path)
        {
            var p = root; // Start from the root Pack.
            foreach (var n in clean_path(ref_path).Split('/')) // Split path into segments.
            {
                var name = brush(n, p.name); // Brush path segment name.
                var next = p.children.FirstOrDefault(pack => pack.name == name); // Check if child Pack exists.

                if (next == null)
                    p.children.Add(next = new Pack { name = name, parent = p }); // Create and add new child Pack if not exists.

                p = next; // Move to the next Pack in the hierarchy.
            }

            return p; // Return the registered Pack.
        }


        /**
         * @brief Writes the Pack's AdHoc definition to a StringBuilder.
         * @param dst The StringBuilder to write to.
         */
        public override void write(StringBuilder dst)
        {
            if (this != root) // Don't write root Pack definition.
                dst.Append($@"
                            {_comment(comment)}
                            {string.Join('\n', attributes)}
                            {string.Join('\n', inherits.Select(i => $"/// <see cref=\"{get(i)}\"/>\n"))}
                            public {(is_enum ? "enum" : "class")} {name} {{
                            "); // Start Pack/enum definition.

            if (is_enum && fields.Count < 2) // Ensure enums have at least two fields for valid AdHoc enum.
                new Field(this, "one_more_field", null); // Add dummy field if enum has only one.

            foreach (var fld in fields) fld.write(dst); // Write Field definitions.

            foreach (var pack in children.Where(p => p.Reference == "")) pack.write(dst); // Recursively write child Pack definitions.

            if (this != root) dst.Append("\n}\n"); // End Pack/enum definition.
        }
    }

    public static Pack root = new Pack(); // Root Pack of the AdHoc protocol definition hierarchy.
    public static List<Stage> stages = [];        // List of stages for client-server channel definition.

    public static string hosts; // String to accumulate host definitions.

    /**
     * @brief Processes OpenAPI Servers information and generates AdHoc Host definitions.
     * @param Servers List of OpenAPI Server objects.
     */
    public static void read_servers(IList<OpenApiServer> Servers)
    {

        var id = 0;
        foreach (var server in Servers) // Iterate through OpenAPI servers.
        {
            hosts += $@"
///<see cref = 'InTS'/>   implementation in TypeScript
///<see cref = 'InCS'/>   implementation in C#
///<see cref = 'InJAVA'/> implementation in JAVA
///<see cref = 'InCPP'/>  implementation in C++
///<see cref = 'InRS'/>   implementation in RUST
///<see cref = 'InGO'/>   implementation in GO
";
            hosts += add_attribute("Server", ["url", "Description"], [$"\"{server.Url}\"", $"\"{server.Description}\""]); // Add Server attribute for each server.
            // Process server variables.
            if (0 < server.Variables?.Count)
                foreach (var (varName, variable) in server.Variables)
                    hosts += add_attribute(brush(varName, ""), ["Description", "Default"], [$"\"{variable.Description}\"", $"\"{variable.Default}\""]); // Add attributes for server variables.

            hosts += $@"
struct  Server{id++}: Host{{}}
"; // Define Server struct inheriting from Host.
        }
    }

    /**
     * @brief Adds client and server branches to the stages list for channel definition.
     * @param client_branch Client branch containing client-side Packs.
     * @param server_branch Server branch containing server-side Packs.
     */
    public static void add_stage(Branch client_branch, Branch server_branch)
    {
        client_branch.dstStage = stages.Count; // Set destination stage index for client branch.
        stages[0].client_branches.Add(client_branch); // Add client branch to the first stage.

        var st = new Stage(); // Create a new stage for the server branch.
        stages.Add(st);
        st.server_branches.Add(server_branch); // Add server branch to the new stage.
    }

    // --- Stage Class (Represents a stage in the client-server channel) ---
    /**
     * @class Stage
     * @brief Represents a stage in the client-server communication channel definition.
     *
     * Contains lists of client and server branches, representing Packs for each side of the communication at this stage.
     */
    public class Stage
    {
        public List<Branch> client_branches = []; // List of client branches (L side of channel).
        public List<Branch> server_branches = []; // List of server branches (R side of channel).
    }

    // --- Branch Class (Represents a branch in a stage, containing Packs) ---
    /**
     * @class Branch
     * @brief Represents a branch within a communication stage, containing a list of Packs.
     *
     * Used to group Packs belonging to either the client or server side at a specific stage.
     */
    public class Branch
    {
        public List<Pack> packs; // List of Packs in this branch.
        public int dstStage; // Destination stage index for this branch.
    }

    /**
     * @brief Brushes a name to ensure it's a valid AdHoc identifier.
     *
     * If the name is a reserved keyword or prohibited, it brushes the name using HasDocs.brush method.
     * Otherwise, returns the name after basic cleaning (trimming, replacing special chars).
     * @param name The name to brush.
     * @param class_name The class name (used for context in HasDocs.brush).
     * @return The brushed name.
     */
    public static string brush(string name, string class_name)
    {
        name = name.Trim().Replace('/', '_').Replace("-", "_").Replace('.', '_').Replace("[", "").Replace("]", "").Replace(" ", "_");


        if (name != class_name && (name.Equals("_DefaultMaxLengthOf") || !HasDocs.is_prohibited(name))) return name; // Return cleaned name if not prohibited.

        return HasDocs.brush(name, class_name); // Brush name using HasDocs if prohibited or reserved.
    }
}