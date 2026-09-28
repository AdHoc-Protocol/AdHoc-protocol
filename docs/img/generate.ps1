# Generates the AdHoc README illustrations as dark-theme SVG.
# Deterministic: the scatter clouds come from a seeded LCG, so re-running yields identical files.

# writes next to itself; run as:  pwsh docs/img/generate.ps1
$out = $PSScriptRoot
New-Item -ItemType Directory -Force -Path $out | Out-Null

# ---------- palette (continues the dark grey / orange / yellow / hatched-blue look of the originals)
$BG    = "#2b2b2b"
$FRAME = "#4a4a4a"
$AXIS  = "#e8833a"   # axes and the declared base line
$WIRE  = "#4fb3ff"   # the quantity that actually travels
$ARROW = "#f2c14e"   # transformation between layers
$CALL  = "#a06fd0"   # callout border
$TXT   = "#e8eaed"
$DIM   = "#9aa0a6"
$DOT   = "#dfe3e8"
$BAND  = "#4a90d9"
$FONT  = "font-family=`"Segoe UI,DejaVu Sans,Helvetica,Arial,sans-serif`""
# keeps labels readable on top of the scatter cloud; degrades to plain text if paint-order is unsupported
$HALO  = "paint-order=`"stroke`" stroke=`"$BG`" stroke-width=`"3.5`" stroke-linejoin=`"round`""

# Arrowheads are drawn as explicit triangles rather than <marker>: GitHub sanitizes SVG served from a
# repository, and a stripped marker would leave a bare line with no head. Every arrow here is strictly
# vertical or horizontal, so no rotation is needed.
$AH = 7.0   # head length
$AW = 4.2   # half-width of the head
function vArrow($x, $y1, $y2, $color, $width) {   # double-headed, y1 -> y2
    $d = if ($y2 -gt $y1) { 1 } else { -1 }
    "<line x1=`"$x`" y1=`"$y1`" x2=`"$x`" y2=`"$y2`" stroke=`"$color`" stroke-width=`"$width`"/>" +
    "<path d=`"M$x $y1 L$($x-$AW) $($y1+$d*$AH) L$($x+$AW) $($y1+$d*$AH) Z`" fill=`"$color`"/>" +
    "<path d=`"M$x $y2 L$($x-$AW) $($y2-$d*$AH) L$($x+$AW) $($y2-$d*$AH) Z`" fill=`"$color`"/>"
}
function hArrow($x1, $x2, $y, $color, $width) {   # double-headed, x1 -> x2
    $d = if ($x2 -gt $x1) { 1 } else { -1 }
    "<line x1=`"$x1`" y1=`"$y`" x2=`"$x2`" y2=`"$y`" stroke=`"$color`" stroke-width=`"$width`"/>" +
    "<path d=`"M$x1 $y L$($x1+$d*$AH) $($y-$AW) L$($x1+$d*$AH) $($y+$AW) Z`" fill=`"$color`"/>" +
    "<path d=`"M$x2 $y L$($x2-$d*$AH) $($y-$AW) L$($x2-$d*$AH) $($y+$AW) Z`" fill=`"$color`"/>"
}

# ---------- deterministic RNG
$script:seed = 20240814
function rnd {
    $script:seed = [int](([long]$script:seed * 1103515245 + 12345) % 2147483648)
    return $script:seed / 2147483648.0
}
# mixture of a tight core and a loose tail - a single exponential looks almost uniform on screen
function falloff {
    $u = rnd; if ($u -le 0.0001) { $u = 0.0001 }
    $rate = if ((rnd) -lt 0.58) { 0.075 } else { 0.30 }
    return -$rate * [Math]::Log($u)
}

function head($w, $h) {
    "<svg xmlns=`"http://www.w3.org/2000/svg`" viewBox=`"0 0 $w $h`" width=`"$w`" height=`"$h`" role=`"img`">" +
    "<rect width=`"$w`" height=`"$h`" fill=`"$BG`"/>"
}

# ---------- one varint scatter plot. $mode: A = mass at the bottom, V = at the top, X = in the middle
function varint($mode, $file) {
    $w = 560; $h = 300
    $L = 78; $R = 468; $T = 58; $B = 240
    $s = New-Object System.Text.StringBuilder
    [void]$s.Append((head $w $h))
    [void]$s.Append("<rect x=`"$L`" y=`"$T`" width=`"$($R-$L)`" height=`"$($B-$T)`" fill=`"none`" stroke=`"$FRAME`" stroke-width=`"1`"/>")

    $span = $B - $T
    switch ($mode) {
        'A' { $baseY = $B; $farY = $T; $baseLbl = "min";  $farLbl = "max" }
        'V' { $baseY = $T; $farY = $B; $baseLbl = "max";  $farLbl = "min" }
        'X' { $baseY = [int](($T + $B) / 2); $farY = $T; $baseLbl = "zero"; $farLbl = "" }
    }

    # ---- scatter cloud
    $near = New-Object System.Text.StringBuilder
    $far  = New-Object System.Text.StringBuilder
    $n = 0
    while ($n -lt 1000) {
        if ($mode -eq 'X') {
            $d = (falloff) / 2.0
            if ((rnd) -lt 0.5) { $d = -$d }
            $frac = 0.5 + $d
            if ($frac -lt 0 -or $frac -gt 1) { continue }
            $y = $T + $frac * $span
        } else {
            $d = falloff
            if ($d -gt 1) { continue }
            $y = if ($mode -eq 'A') { $B - $d * $span } else { $T + $d * $span }
        }
        $x = $L + 2 + (rnd) * ($R - $L - 4)
        $c = "<circle cx=`"$([Math]::Round($x,1))`" cy=`"$([Math]::Round($y,1))`" "
        if ($n % 2 -eq 0) { [void]$near.Append($c + "r=`"1`"/>") } else { [void]$far.Append($c + "r=`".7`"/>") }
        $n++
    }
    [void]$s.Append("<g fill=`"$DOT`" opacity=`".85`">$near</g><g fill=`"$DOT`" opacity=`".4`">$far</g>")

    # ---- the declared base line, on top of the cloud
    [void]$s.Append("<line x1=`"$L`" y1=`"$baseY`" x2=`"$($R+14)`" y2=`"$baseY`" stroke=`"$AXIS`" stroke-width=`"2.5`"/>")
    [void]$s.Append("<text x=`"$($R+20)`" y=`"$($baseY+4)`" $FONT font-size=`"13`" font-weight=`"600`" fill=`"$AXIS`">$baseLbl</text>")
    if ($farLbl) {
        [void]$s.Append("<line x1=`"$L`" y1=`"$farY`" x2=`"$($R+14)`" y2=`"$farY`" stroke=`"$DIM`" stroke-width=`"1`" stroke-dasharray=`"4 4`"/>")
        [void]$s.Append("<text x=`"$($R+20)`" y=`"$($farY+4)`" $FONT font-size=`"13`" fill=`"$DIM`">$farLbl</text>")
    }

    # ---- amplitude bracket for X
    if ($mode -eq 'X') {
        foreach ($yy in @($T, $B)) {
            [void]$s.Append("<line x1=`"$L`" y1=`"$yy`" x2=`"$($R+14)`" y2=`"$yy`" stroke=`"$DIM`" stroke-width=`"1`" stroke-dasharray=`"4 4`"/>")
        }
        [void]$s.Append((vArrow ($R+54) ($T+5) ($baseY-5) $DIM "1.2"))
        [void]$s.Append("<text x=`"$($R+60)`" y=`"$($T+($baseY-$T)/2+4)`" $FONT font-size=`"12`" fill=`"$DIM`">ampl</text>")
    }

    # ---- what actually travels: a typical distance and a rare one
    $dir   = if ($mode -eq 'V') { 1 } else { -1 }
    $xTyp  = $L + 112
    $xRare = $L + 286
    $yTyp  = $baseY + $dir * 30
    $yRare = if ($mode -eq 'X') { $baseY - $dir * 80 } else { $baseY + $dir * 158 }
    [void]$s.Append((vArrow $xTyp  $baseY $yTyp  $WIRE "1.6"))
    [void]$s.Append((vArrow $xRare $baseY $yRare $WIRE "1.6"))

    $yTypLbl  = if ($mode -eq 'V') { $yTyp + 17 } else { $yTyp - 9 }
    $yRareLbl = if ($mode -eq 'V') { $yRare + 17 } else { $yRare - 9 }
    [void]$s.Append("<text x=`"$($xTyp+7)`" y=`"$yTypLbl`" $FONT font-size=`"12`" fill=`"$WIRE`" $HALO>typical: 1 byte</text>")
    [void]$s.Append("<text x=`"$($xRare+7)`" y=`"$yRareLbl`" $FONT font-size=`"12`" fill=`"$WIRE`" $HALO>rare: more bytes</text>")

    # ---- captions
    [void]$s.Append("<text x=`"$L`" y=`"38`" $FONT font-size=`"17`" font-weight=`"700`" fill=`"$TXT`">[$mode]</text>")
    [void]$s.Append("<text x=`"$($L+34)`" y=`"38`" $FONT font-size=`"12`" fill=`"$DIM`">on the wire: the distance from the " +
                    "<tspan fill=`"$AXIS`">$baseLbl</tspan> line</text>")
    [void]$s.Append("<text x=`"$L`" y=`"$($B+26)`" $FONT font-size=`"12`" fill=`"$DIM`">time \u2192</text>")
    $mid = $T + ($B - $T) / 2
    [void]$s.Append("<text x=`"$($L-12)`" y=`"$mid`" $FONT font-size=`"12`" fill=`"$DIM`" text-anchor=`"middle`" transform=`"rotate(-90 $($L-12) $mid)`">value</text>")
    [void]$s.Append("<text x=`"$($R-2)`" y=`"$($B+26)`" $FONT font-size=`"11`" fill=`"$DIM`" text-anchor=`"end`">density = how often that value occurs</text>")
    [void]$s.Append("</svg>")

    $svg = ($s.ToString() -replace '\\u2192', [char]0x2192)
    [IO.File]::WriteAllText((Join-Path $out $file), $svg, (New-Object Text.UTF8Encoding $false))
    "$file  ($($svg.Length) bytes)"
}

varint 'A' "varint-a.svg"
varint 'V' "varint-v.svg"
varint 'X' "varint-x.svg"

# ---------- the failure mode: a narrow cluster sitting far from a base left at zero
function trap_plot($file) {
    $w = 560; $h = 300
    $L = 78; $R = 468; $T = 58; $B = 240
    $s = New-Object System.Text.StringBuilder
    [void]$s.Append((head $w $h))
    [void]$s.Append("<rect x=`"$L`" y=`"$T`" width=`"$($R-$L)`" height=`"$($B-$T)`" fill=`"none`" stroke=`"$FRAME`" stroke-width=`"1`"/>")

    $bandY = $T + 34          # the cluster rides high above zero
    $near = New-Object System.Text.StringBuilder
    $far  = New-Object System.Text.StringBuilder
    for ($n = 0; $n -lt 620; $n++) {
        $y = $bandY + ((rnd) + (rnd) - 1) * 11      # triangular spread: a tight, natural-looking band
        $x = $L + 2 + (rnd) * ($R - $L - 4)
        $c = "<circle cx=`"$([Math]::Round($x,1))`" cy=`"$([Math]::Round($y,1))`" "
        if ($n % 2 -eq 0) { [void]$near.Append($c + "r=`"1`"/>") } else { [void]$far.Append($c + "r=`".7`"/>") }
    }
    [void]$s.Append("<g fill=`"$DOT`" opacity=`".85`">$near</g><g fill=`"$DOT`" opacity=`".4`">$far</g>")

    # zero - the base you get when you declare none
    [void]$s.Append("<line x1=`"$L`" y1=`"$B`" x2=`"$($R+14)`" y2=`"$B`" stroke=`"$DIM`" stroke-width=`"2`"/>")
    [void]$s.Append("<text x=`"$($R+20)`" y=`"$($B+4)`" $FONT font-size=`"13`" fill=`"$DIM`">0</text>")
    # the base that belongs under the cluster
    $declared = $bandY + 17
    [void]$s.Append("<line x1=`"$L`" y1=`"$declared`" x2=`"$($R+14)`" y2=`"$declared`" stroke=`"$AXIS`" stroke-width=`"2`" stroke-dasharray=`"6 4`"/>")
    [void]$s.Append("<text x=`"$($R+20)`" y=`"$($declared+4)`" $FONT font-size=`"12`" font-weight=`"600`" fill=`"$AXIS`">base</text>")

    # what each choice costs
    [void]$s.Append((vArrow ($L+128) $B ($bandY+4) $WIRE "1.6"))
    [void]$s.Append("<text x=`"$($L+136)`" y=`"$($T+128)`" $FONT font-size=`"12`" fill=`"$WIRE`" $HALO>no base: 5 bytes -</text>")
    [void]$s.Append("<text x=`"$($L+136)`" y=`"$($T+144)`" $FONT font-size=`"12`" fill=`"$WIRE`" $HALO>every value, every packet</text>")
    [void]$s.Append((vArrow ($L+320) $declared ($bandY-6) $WIRE "1.6"))
    [void]$s.Append("<text x=`"$($L+312)`" y=`"$($declared+20)`" $FONT font-size=`"12`" fill=`"$WIRE`" text-anchor=`"end`" $HALO>base under the cluster: 1 byte</text>")

    [void]$s.Append("<text x=`"$L`" y=`"38`" $FONT font-size=`"14`" font-weight=`"700`" fill=`"$TXT`">the trap:</text>")
    [void]$s.Append("<text x=`"$($L+66)`" y=`"38`" $FONT font-size=`"12`" fill=`"$DIM`">the cluster is narrow, but nothing about it is near zero</text>")
    [void]$s.Append("<text x=`"$L`" y=`"$($B+26)`" $FONT font-size=`"12`" fill=`"$DIM`">time →</text>")
    $mid = $T + ($B - $T) / 2
    [void]$s.Append("<text x=`"$($L-12)`" y=`"$mid`" $FONT font-size=`"12`" fill=`"$DIM`" text-anchor=`"middle`" transform=`"rotate(-90 $($L-12) $mid)`">value</text>")
    [void]$s.Append("<text x=`"$($R-2)`" y=`"$($B+26)`" $FONT font-size=`"11`" fill=`"$DIM`" text-anchor=`"end`">varint sends the distance, not the spread</text>")
    [void]$s.Append("</svg>")

    $svg = ($s.ToString() -replace '\\u2192', [char]0x2192)
    [IO.File]::WriteAllText((Join-Path $out $file), $svg, (New-Object Text.UTF8Encoding $false))
    "$file  ($($svg.Length) bytes)"
}

trap_plot "varint-base-trap.svg"

# ---------- the exT / inT / ioT layer figures
function layers($file, $w, $h, $cols, $callouts) {
    $s = New-Object System.Text.StringBuilder
    [void]$s.Append((head $w $h))
    [void]$s.Append("<defs><pattern id=`"hatch`" width=`"6`" height=`"6`" patternTransform=`"rotate(45)`" patternUnits=`"userSpaceOnUse`">" +
                    "<line x1=`"0`" y1=`"0`" x2=`"0`" y2=`"6`" stroke=`"#ffffff`" stroke-width=`"3`"/></pattern></defs>")

    $zeroY = $h - 108
    $topY  = if ($callouts) { 196 } else { 74 }
    $axBot = $h - 34
    $n = $cols.Count
    $step = [int](($w - 28) / $n)

    [void]$s.Append("<line x1=`"34`" y1=`"$zeroY`" x2=`"$($w-24)`" y2=`"$zeroY`" stroke=`"$DIM`" stroke-width=`"1.2`"/>")

    for ($i = 0; $i -lt $n; $i++) {
        $c = $cols[$i]
        $x = [int]($step * ($i + 0.5)) + 14
        [void]$s.Append("<line x1=`"$x`" y1=`"$topY`" x2=`"$x`" y2=`"$axBot`" stroke=`"$AXIS`" stroke-width=`"1.6`"/>")
        [void]$s.Append("<text x=`"$x`" y=`"$($topY-30)`" $FONT font-size=`"17`" font-weight=`"700`" fill=`"$TXT`" text-anchor=`"middle`">$($c.name)</text>")
        $sizeFill = if ($c.hot) { $WIRE } else { $DIM }
        [void]$s.Append("<text x=`"$x`" y=`"$($topY-10)`" $FONT font-size=`"15`" fill=`"$TXT`" text-anchor=`"middle`">$($c.type) " +
                        "<tspan font-size=`"12`" fill=`"$sizeFill`">$($c.size)</tspan></text>")

        $bandTop = [Math]::Round($zeroY - $c.top * ($zeroY - $topY), 1)
        $bandBot = [Math]::Round($zeroY - $c.bottom * ($zeroY - $topY), 1)
        $bh = [Math]::Max(10, $bandBot - $bandTop)
        foreach ($fill in @("$BAND", "url(#hatch)")) {
            $op = if ($fill -eq "$BAND") { "1" } else { ".7" }
            [void]$s.Append("<rect x=`"$($x-11)`" y=`"$bandTop`" width=`"22`" height=`"$bh`" fill=`"$fill`" opacity=`"$op`" stroke=`"$TXT`" stroke-width=`"1`"/>")
        }
        [void]$s.Append("<text x=`"$($x-17)`" y=`"$($bandTop+4)`" $FONT font-size=`"12`" fill=`"$TXT`" text-anchor=`"end`" $HALO>$($c.hi)</text>")
        if ($c.lo) { [void]$s.Append("<text x=`"$($x-17)`" y=`"$($bandBot+4)`" $FONT font-size=`"12`" fill=`"$TXT`" text-anchor=`"end`" $HALO>$($c.lo)</text>") }
        if ($c.zero) { [void]$s.Append("<text x=`"$($x+15)`" y=`"$($zeroY+15)`" $FONT font-size=`"12`" fill=`"$DIM`">0</text>") }

        if ($i -lt $n - 1) {
            $ax = [int]($step * ($i + 1)) + 14
            [void]$s.Append((hArrow ($ax-32) ($ax+32) ($topY-56) $ARROW "2"))
        }
    }

    if ($callouts) {
        $bw = [int](($w - 60) / 2)
        for ($i = 0; $i -lt 2; $i++) {
            $bx = 24 + $i * ($bw + 12)
            $lines = $callouts[$i]
            $bh2 = 26 + $lines.Count * 20
            [void]$s.Append("<rect x=`"$bx`" y=`"18`" width=`"$bw`" height=`"$bh2`" rx=`"3`" fill=`"#242424`" stroke=`"$CALL`" stroke-width=`"1.6`"/>")
            for ($k = 0; $k -lt $lines.Count; $k++) {
                [void]$s.Append("<text x=`"$($bx+12)`" y=`"$(40 + $k*20)`" $FONT font-size=`"14`" fill=`"$TXT`">$($lines[$k])</text>")
            }
            $ax = [int]($step * ($i + 1)) + 14
            [void]$s.Append("<line x1=`"$([int]($bx + $bw/2))`" y1=`"$($bh2+18)`" x2=`"$ax`" y2=`"$($topY-62)`" stroke=`"$CALL`" stroke-width=`"1.2`"/>")
        }
    }

    [void]$s.Append("</svg>")
    [IO.File]::WriteAllText((Join-Path $out $file), $s.ToString(), (New-Object Text.UTF8Encoding $false))
    "$file  ($($s.Length) bytes)"
}

layers "value-layers-transform.svg" 620 470 @(
    @{ name="exT"; type="long"; size="8 bytes"; hot=$false; hi="40 000 000 093"; lo="40 000 000 000"; top=0.92; bottom=0.72; zero=$false },
    @{ name="inT"; type="byte"; size="1 byte";  hot=$true;  hi="93"; lo=$null; top=0.26; bottom=0.0; zero=$true },
    @{ name="ioT"; type="byte"; size="1 byte";  hot=$true;  hi="93"; lo=$null; top=0.26; bottom=0.0; zero=$true }
) @(
    @("The transformation runs when", "external code touches the", "field. Its only purpose is", "to shrink storage."),
    @("This transformation runs on", "every send and on every", "receive.")
)

layers "value-layers-quantization.svg" 620 380 @(
    @{ name="exT"; type="int"; size="4 bytes"; hot=$false; hi="1 080 000"; lo="1 000 000"; top=0.94; bottom=0.70; zero=$false },
    @{ name="inT"; type="int"; size="4 bytes"; hot=$false; hi="80 000"; lo=$null; top=0.24; bottom=0.0; zero=$true },
    @{ name="ioT"; type="int"; size="3 bytes"; hot=$true;  hi="80 000"; lo=$null; top=0.24; bottom=0.0; zero=$true }
) $null

# ═══════════════════════════════════════════════════════════════════════════════
#  Multiplexing: one port, many Connections.
#  A declared Connects<L,R> is a typed link between two hosts; how many ports
#  carry a host's Connections is a deployment choice. Multiplex<(C1, C2, …)> lets
#  the Connections sharing one host share one port: every socket opens with one
#  byte - the uid of the dialing host - and the port picks the Connection by it.
#
#  Visual language: a host is a blue box; a port is a plate laid over its face,
#  marked with the service symbol; behind every port sits one TCP/WebSocket
#  server instance; the multiplexer is the yellow-edged box that reads the first
#  byte; a Connection kind is the small dark box; Connection names are yellow,
#  the first byte and callouts are purple.
# ═══════════════════════════════════════════════════════════════════════════════

$HOSTC = "#3a5a80"   # host box fill
$EDGE  = "#6f9fd8"   # host box stroke
$PORTC = "#243447"   # port fill
$SRVC  = "#2d4665"   # transport server instance fill, inside the host
$MUXC  = "#4a3d1c"   # multiplexer fill
$CONNC = "#26384d"   # a Connection kind, inside the host

function nbox($x, $y, $w, $h, $fill, $stroke, $label, $sub) {
    $cx = [int]($x + $w / 2)
    $s = "<rect x=`"$x`" y=`"$y`" width=`"$w`" height=`"$h`" rx=`"4`" fill=`"$fill`" stroke=`"$stroke`" stroke-width=`"1.6`"/>"
    if ($sub) {
        $s += "<text x=`"$cx`" y=`"$([int]($y + $h/2 - 1))`" $FONT font-size=`"14`" fill=`"$TXT`" text-anchor=`"middle`">$label</text>"
        $s += "<text x=`"$cx`" y=`"$([int]($y + $h/2 + 16))`" $FONT font-size=`"11`" fill=`"$DIM`" text-anchor=`"middle`">$sub</text>"
    }
    else {
        $s += "<text x=`"$cx`" y=`"$([int]($y + $h/2 + 5))`" $FONT font-size=`"14`" fill=`"$TXT`" text-anchor=`"middle`">$label</text>"
    }
    return $s
}

function txt($x, $y, $size, $color, $anchor, $text) {
    "<text x=`"$x`" y=`"$y`" $FONT font-size=`"$size`" fill=`"$color`" text-anchor=`"$anchor`" $HALO>$text</text>"
}

function line($x1, $y1, $x2, $y2, $color, $width) {
    "<line x1=`"$x1`" y1=`"$y1`" x2=`"$x2`" y2=`"$y2`" stroke=`"$color`" stroke-width=`"$width`"/>"
}

# single-headed horizontal arrow, head at x2
function hArrow1($x1, $x2, $y, $color, $width) {
    $d = if ($x2 -gt $x1) { 1 } else { -1 }
    (line $x1 $y $x2 $y $color $width) +
    "<path d=`"M$x2 $y L$($x2-$d*$AH) $($y-$AW) L$($x2-$d*$AH) $($y+$AW) Z`" fill=`"$color`"/>"
}

# the service marker: an arrow in a circle - something is listening here
function svcicon($cx, $cy, $r, $color) {
    "<circle cx=`"$cx`" cy=`"$cy`" r=`"$r`" fill=`"none`" stroke=`"$color`" stroke-width=`"1.4`"/>" +
    "<path d=`"M$([int]($cx - $r*0.3)) $([int]($cy - $r*0.5)) L$([int]($cx + $r*0.5)) $cy L$([int]($cx - $r*0.3)) $([int]($cy + $r*0.5)) Z`" fill=`"$color`"/>"
}

# a port: the plate laid over the host face, service marker beside the number
function portbox($x, $y, $w, $h, $label, $ir, $fs) {
    $cy = [int]($y + $h / 2)
    $tx = [int]($x + $ir * 2 + 10 + ($w - $ir * 2 - 10) / 2)
    "<rect x=`"$x`" y=`"$y`" width=`"$w`" height=`"$h`" rx=`"4`" fill=`"$PORTC`" stroke=`"$AXIS`" stroke-width=`"1.8`"/>" +
    (svcicon ([int]($x + $ir + 8)) $cy $ir $AXIS) +
    "<text x=`"$tx`" y=`"$($cy + 5)`" $FONT font-size=`"$fs`" fill=`"$TXT`" text-anchor=`"middle`">$label</text>"
}

# a TCP/WebSocket server instance sitting inside the host, behind one port
function srvbox($x, $y, $w, $h, $size, $label) {
    "<rect x=`"$x`" y=`"$y`" width=`"$w`" height=`"$h`" rx=`"3`" fill=`"$SRVC`" stroke=`"$EDGE`" stroke-width=`"1.2`" stroke-dasharray=`"5 3`"/>" +
    "<text x=`"$([int]($x + $w/2))`" y=`"$([int]($y + $h/2 + 4))`" $FONT font-size=`"$size`" fill=`"$TXT`" text-anchor=`"middle`">$label</text>"
}

# the multiplexer: reads the first byte, hands the socket to a Connection
function muxbox($x, $y, $w, $h, $label, $size) {
    "<rect x=`"$x`" y=`"$y`" width=`"$w`" height=`"$h`" rx=`"4`" fill=`"$MUXC`" stroke=`"$ARROW`" stroke-width=`"1.8`"/>" +
    "<text x=`"$([int]($x + $w/2))`" y=`"$([int]($y + $h/2 + 4))`" $FONT font-size=`"$size`" fill=`"$TXT`" text-anchor=`"middle`">$label</text>"
}

# a Connection kind inside the host
function connbox($x, $y, $w, $h, $label, $sub) {
    $cx = [int]($x + $w / 2)
    $s = "<rect x=`"$x`" y=`"$y`" width=`"$w`" height=`"$h`" rx=`"3`" fill=`"$CONNC`" stroke=`"$EDGE`" stroke-width=`"1.2`"/>"
    if ($sub) {
        $s += "<text x=`"$cx`" y=`"$([int]($y + $h/2 - 2))`" $FONT font-size=`"12.5`" fill=`"$ARROW`" text-anchor=`"middle`">$label</text>"
        $s += "<text x=`"$cx`" y=`"$([int]($y + $h/2 + 13))`" $FONT font-size=`"10.5`" fill=`"$DIM`" text-anchor=`"middle`">$sub</text>"
    }
    else {
        $s += "<text x=`"$cx`" y=`"$([int]($y + $h/2 + 4))`" $FONT font-size=`"12.5`" fill=`"$ARROW`" text-anchor=`"middle`">$label</text>"
    }
    return $s
}

function save($s, $file) {
    [void]$s.Append("</svg>")
    [IO.File]::WriteAllText((Join-Path $out $file), $s.ToString(), (New-Object Text.UTF8Encoding $false))
    "$file  ($($s.Length) bytes)"
}

# ---------- four kinds of peer, one host, one port: the first byte picks the Connection
function mux_one_port($file) {
    $w = 980; $h = 500
    $s = New-Object System.Text.StringBuilder
    [void]$s.Append((head $w $h))
    [void]$s.Append("<title>Four peer hosts dial one host, Fleet, through one port; the multiplexer FleetPort reads the first byte of each socket - the dialing host's uid - and hands the socket to the matching Connection</title>")

    $rows = @(
        @{ y = 44;  host = "Vehicle";       sub = "firmware 3.x - most of the fleet"; conn = "Telemetry";       uid = "Vehicle.uid" },
        @{ y = 124; host = "VehicleLegacy"; sub = "firmware 2.x - not yet reflashed"; conn = "TelemetryLegacy"; uid = "VehicleLegacy.uid" },
        @{ y = 234; host = "Dashboard";     sub = "operator console, browser";        conn = "Console";         uid = "Dashboard.uid" },
        @{ y = 314; host = "Service";       sub = "field diagnostic tool";            conn = "Diagnostics";     uid = "Service.uid" }
    )
    $bx = 24; $bw = 196; $bh = 50
    $gather = 392
    $py = 204; $ph = 44; $pcy = 226                       # the port plate
    $hx = 460; $hy = 30; $hw = 500; $hh = 400             # the Fleet host

    [void]$s.Append("<rect x=`"$hx`" y=`"$hy`" width=`"$hw`" height=`"$hh`" rx=`"4`" fill=`"$HOSTC`" stroke=`"$EDGE`" stroke-width=`"1.6`"/>")
    [void]$s.Append((txt 548 62 15 $TXT "middle" "Fleet"))
    [void]$s.Append((txt 548 80 11 $DIM "middle" "one host, four Connections"))

    # peers and their links
    [void]$s.Append((line $gather ($rows[0].y + 25) $gather ($rows[3].y + 25) $WIRE 2))
    foreach ($r in $rows) {
        $cy = [int]($r.y + $bh / 2)
        [void]$s.Append((nbox $bx $r.y $bw $bh $HOSTC $EDGE $r.host $r.sub))
        [void]$s.Append((line ($bx + $bw) $cy $gather $cy $WIRE 2))
        [void]$s.Append((txt ([int](($bx + $bw + $gather) / 2)) ($cy - 9) 12.5 $ARROW "middle" $r.conn))
    }
    [void]$s.Append("<path d=`"M16 50 L8 50 L8 168 L16 168`" fill=`"none`" stroke=`"$CALL`" stroke-width=`"1.6`"/>")
    [void]$s.Append((txt 24 205 11.5 $CALL "start" "two firmware generations = two hosts, two Connections"))

    # the port, the server instance, the multiplexer
    [void]$s.Append((line $gather $pcy 420 $pcy $WIRE 3))
    [void]$s.Append((portbox 420 $py 84 $ph ":443" 8 14))
    [void]$s.Append((line 504 $pcy 520 $pcy $WIRE 2))
    [void]$s.Append((srvbox 520 206 112 40 11 "TCP/WS server"))
    [void]$s.Append((line 632 $pcy 652 $pcy $WIRE 2))

    $mx = 652; $my = 100; $mw = 150; $mh = 300
    [void]$s.Append("<rect x=`"$mx`" y=`"$my`" width=`"$mw`" height=`"$mh`" rx=`"4`" fill=`"$MUXC`" stroke=`"$ARROW`" stroke-width=`"1.8`"/>")
    [void]$s.Append((txt 727 124 14 $TXT "middle" "FleetPort"))
    [void]$s.Append((txt 727 141 10.5 $DIM "middle" "reads the 1st byte"))

    $conns = @(
        @{ cy = 176; uid = "Vehicle.uid";       conn = "Telemetry" },
        @{ cy = 236; uid = "VehicleLegacy.uid"; conn = "TelemetryLegacy" },
        @{ cy = 296; uid = "Dashboard.uid";     conn = "Console" },
        @{ cy = 356; uid = "Service.uid";       conn = "Diagnostics" }
    )
    foreach ($c in $conns) {
        [void]$s.Append("<rect x=`"662`" y=`"$($c.cy - 13)`" width=`"130`" height=`"26`" rx=`"3`" fill=`"$BG`" stroke=`"$CALL`" stroke-width=`"1.2`"/>")
        [void]$s.Append((txt 727 ($c.cy + 4) 11 $TXT "middle" $c.uid))
        [void]$s.Append((hArrow1 792 822 $c.cy $WIRE 2))
        [void]$s.Append((connbox 822 ($c.cy - 22) 126 44 $c.conn "pooled"))
    }

    [void]$s.Append((txt 490 460 14 $TXT "middle" "Four declared Connections. One host, one port, one server instance."))
    [void]$s.Append((txt 490 482 12 $DIM "middle" "Every socket opens with one byte - the uid of the dialing host - and the multiplexer hands it to that Connection."))
    save $s $file
}

# ---------- the three shapes a host's Connections can take on its ports
function mux_shapes($file) {
    $w = 940; $h = 340
    $s = New-Object System.Text.StringBuilder
    [void]$s.Append((head $w $h))
    [void]$s.Append("<title>One host, Fleet, deployed three ways: a port per Connection, one multiplexed port for all of them, and a mix of both</title>")

    $panels = @(
        @{ x = 16;  title = "A port per Connection";   n1 = "a server instance per Connection";   n2 = "no multiplexer declared, or not used" },
        @{ x = 324; title = "One multiplexed port";    n1 = "one server instance for all of them"; n2 = "the 1st byte picks the Connection" },
        @{ x = 632; title = "Mixed";                   n1 = "the public Connections share a port"; n2 = "Diagnostics keeps its own, LAN only" }
    )
    $pw = 292
    foreach ($p in $panels) {
        [void]$s.Append("<rect x=`"$($p.x)`" y=`"16`" width=`"$pw`" height=`"308`" rx=`"5`" fill=`"none`" stroke=`"$FRAME`" stroke-width=`"1`"/>")
        [void]$s.Append((txt ([int]($p.x + $pw/2)) 42 14 $TXT "middle" $p.title))
        [void]$s.Append((txt ([int]($p.x + $pw/2)) 294 11.5 $DIM "middle" $p.n1))
        [void]$s.Append((txt ([int]($p.x + $pw/2)) 311 11.5 $DIM "middle" $p.n2))
        [void]$s.Append("<rect x=`"$($p.x+150)`" y=`"58`" width=`"132`" height=`"212`" rx=`"4`" fill=`"$HOSTC`" stroke=`"$EDGE`" stroke-width=`"1.6`"/>")
        [void]$s.Append((txt ($p.x + 216) 262 13 $TXT "middle" "Fleet"))
    }

    # ---- A: each Connection on a port of its own
    $a = 16
    foreach ($r in @(@{ y = 76; peer = "Vehicle"; port = ":443"; conn = "Telemetry" }, @{ y = 172; peer = "Dashboard"; port = ":444"; conn = "Console" })) {
        $cy = $r.y + 17
        [void]$s.Append((nbox ($a + 8) $r.y 84 34 $HOSTC $EDGE $r.peer $null))
        [void]$s.Append((line ($a + 92) $cy ($a + 116) $cy $WIRE 2))
        [void]$s.Append((portbox ($a + 116) $r.y 62 34 $r.port 6 12))
        [void]$s.Append((line ($a + 178) $cy ($a + 184) $cy $WIRE 2))
        [void]$s.Append((srvbox ($a + 184) ($r.y - 6) 88 22 9.5 "TCP/WS server"))
        [void]$s.Append((connbox ($a + 184) ($r.y + 20) 88 26 $r.conn $null))
    }

    # ---- B: one multiplexed port
    $b = 324
    [void]$s.Append((nbox ($b + 8) 76 84 34 $HOSTC $EDGE "Vehicle" $null))
    [void]$s.Append((nbox ($b + 8) 172 84 34 $HOSTC $EDGE "Dashboard" $null))
    [void]$s.Append((line ($b + 92) 93 ($b + 104) 93 $WIRE 2))
    [void]$s.Append((line ($b + 92) 189 ($b + 104) 189 $WIRE 2))
    [void]$s.Append((line ($b + 104) 93 ($b + 104) 189 $WIRE 2))
    [void]$s.Append((line ($b + 104) 141 ($b + 116) 141 $WIRE 3))
    [void]$s.Append((portbox ($b + 116) 124 62 34 ":443" 6 12))
    [void]$s.Append((line ($b + 178) 141 ($b + 184) 141 $WIRE 2))
    [void]$s.Append((srvbox ($b + 184) 66 88 22 9.5 "TCP/WS server"))
    [void]$s.Append((line ($b + 228) 88 ($b + 228) 197 $WIRE 2))
    [void]$s.Append((muxbox ($b + 184) 98 88 24 "FleetPort" 11))
    [void]$s.Append((connbox ($b + 184) 146 88 26 "Telemetry" $null))
    [void]$s.Append((connbox ($b + 184) 184 88 26 "Console" $null))

    # ---- C: Vehicle and Dashboard multiplexed, Service on its own port
    $c = 632
    [void]$s.Append((nbox ($c + 8) 66 84 30 $HOSTC $EDGE "Vehicle" $null))
    [void]$s.Append((nbox ($c + 8) 126 84 30 $HOSTC $EDGE "Dashboard" $null))
    [void]$s.Append((nbox ($c + 8) 214 84 30 $HOSTC $EDGE "Service" $null))
    [void]$s.Append((line ($c + 92) 81 ($c + 104) 81 $WIRE 2))
    [void]$s.Append((line ($c + 92) 141 ($c + 104) 141 $WIRE 2))
    [void]$s.Append((line ($c + 104) 81 ($c + 104) 141 $WIRE 2))
    [void]$s.Append((line ($c + 104) 111 ($c + 116) 111 $WIRE 3))
    [void]$s.Append((portbox ($c + 116) 94 62 34 ":443" 6 12))
    [void]$s.Append((line ($c + 178) 111 ($c + 184) 111 $WIRE 2))
    [void]$s.Append((srvbox ($c + 184) 66 88 22 9.5 "TCP/WS server"))
    [void]$s.Append((line ($c + 228) 88 ($c + 228) 168 $WIRE 2))
    [void]$s.Append((muxbox ($c + 184) 98 88 24 "FleetPort" 11))
    [void]$s.Append((connbox ($c + 184) 128 88 24 "Telemetry" $null))
    [void]$s.Append((connbox ($c + 184) 156 88 24 "Console" $null))
    [void]$s.Append((line ($c + 92) 229 ($c + 116) 229 $WIRE 2))
    [void]$s.Append((portbox ($c + 116) 212 62 34 ":8443" 6 11))
    [void]$s.Append((line ($c + 178) 229 ($c + 184) 229 $WIRE 2))
    [void]$s.Append((srvbox ($c + 184) 196 88 22 9.5 "TCP/WS server"))
    [void]$s.Append((connbox ($c + 184) 222 88 24 "Diagnostics" $null))

    save $s $file
}

# ---------- the life of one socket on a multiplexed port
function mux_socket($file) {
    $w = 940; $h = 420
    $s = New-Object System.Text.StringBuilder
    [void]$s.Append((head $w $h))
    [void]$s.Append("<title>The life of one socket on a multiplexed port: the caller connects and sends one byte, the multiplexer takes a Connection from its pool and seats it on the socket, packs flow with no per-pack overhead, and on close the Connection goes back to the pool</title>")

    $L = 150; $M = 470; $R = 790
    [void]$s.Append((nbox 40 20 220 50 $HOSTC $EDGE "Vehicle" "Telemetry.Connection, mux = true"))
    [void]$s.Append((nbox 360 20 220 50 $HOSTC $EDGE "Fleet :443" "TCP/WS server + FleetPort"))
    [void]$s.Append((nbox 680 20 220 50 $HOSTC $EDGE "Fleet" "Telemetry.Connection, pooled"))
    foreach ($x in @($L, $M, $R)) {
        [void]$s.Append("<line x1=`"$x`" y1=`"70`" x2=`"$x`" y2=`"372`" stroke=`"$FRAME`" stroke-width=`"1.4`" stroke-dasharray=`"5 4`"/>")
    }

    $n = 0
    function step($y, $label) {
        $script:n++
        "<circle cx=`"24`" cy=`"$y`" r=`"10`" fill=`"none`" stroke=`"$DIM`" stroke-width=`"1.2`"/>" +
        (txt 24 ($y + 4) 11 $DIM "middle" $script:n)
    }
    $script:n = 0

    [void]$s.Append((step 108 ""))
    [void]$s.Append((hArrow1 $L $M 108 $WIRE 2))
    [void]$s.Append((txt 310 100 12 $TXT "middle" "connect - TCP, or TCP + WebSocket upgrade"))

    [void]$s.Append((step 162 ""))
    [void]$s.Append((hArrow1 $L $M 162 $CALL 2.4))
    [void]$s.Append((txt 310 154 12.5 $CALL "middle" "1st byte: Telemetry.id() = Vehicle.uid"))
    [void]$s.Append((txt 310 180 11 $DIM "middle" "on every socket, reconnects included"))

    [void]$s.Append((step 222 ""))
    [void]$s.Append((hArrow1 $M $R 222 $ARROW 2))
    [void]$s.Append((txt 630 214 12 $ARROW "middle" "FleetPort.apply(Vehicle.uid)"))
    [void]$s.Append((txt 630 240 11 $DIM "middle" "pool.acquire() - seated on the socket - OnOpen()"))

    [void]$s.Append((step 290 ""))
    [void]$s.Append((hArrow $L $R 290 $WIRE 3))
    [void]$s.Append((txt 470 282 12.5 $TXT "middle" "packs, both ways - nothing added per pack"))

    [void]$s.Append((step 346 ""))
    [void]$s.Append((hArrow1 $M $R 346 $DIM 2))
    [void]$s.Append((txt 630 338 12 $TXT "middle" "socket closed - External(null)"))
    [void]$s.Append((txt 630 364 11 $DIM "middle" "back to the pool, ready for the next caller"))

    [void]$s.Append((txt 470 404 12 $DIM "middle" "Unknown byte: the socket is closed and the failure reported. A direct port skips steps 2-3 and has no pool."))
    save $s $file
}

# ---------- two server generations on one port: two multiplexers, one composed function
function mux_generations($file) {
    $w = 980; $h = 480
    $s = New-Object System.Text.StringBuilder
    [void]$s.Append((head $w $h))
    [void]$s.Append("<title>Two server generations, Fleet and FleetV2, in one process behind one port: a composed function asks FleetPort first and FleetV2Port second; every peer host has its own uid, so the byte never needs a guess</title>")

    $rows = @(
        @{ y = 50;  host = "Vehicle";     conn = "Telemetry" },
        @{ y = 120; host = "Dashboard";   conn = "Console" },
        @{ y = 250; host = "VehicleV2";   conn = "TelemetryV2" },
        @{ y = 320; host = "DashboardV2"; conn = "ConsoleV2" }
    )
    $bx = 24; $bw = 150; $bh = 44
    $gather = 340
    [void]$s.Append((line $gather 72 $gather 342 $WIRE 2))
    foreach ($r in $rows) {
        $cy = [int]($r.y + $bh / 2)
        [void]$s.Append((nbox $bx $r.y $bw $bh $HOSTC $EDGE $r.host $null))
        [void]$s.Append((line ($bx + $bw) $cy $gather $cy $WIRE 2))
        [void]$s.Append((txt ([int](($bx + $bw + $gather) / 2)) ($cy - 8) 12 $ARROW "middle" $r.conn))
    }
    [void]$s.Append("<path d=`"M16 54 L8 54 L8 160 L16 160`" fill=`"none`" stroke=`"$CALL`" stroke-width=`"1.6`"/>")
    [void]$s.Append((txt 24 192 11.5 $CALL "start" "generation 1 - frozen, still in the field"))
    [void]$s.Append("<path d=`"M16 254 L8 254 L8 360 L16 360`" fill=`"none`" stroke=`"$CALL`" stroke-width=`"1.6`"/>")
    [void]$s.Append((txt 24 392 11.5 $CALL "start" "generation 2 - free to change"))

    # one process
    [void]$s.Append("<rect x=`"420`" y=`"24`" width=`"544`" height=`"396`" rx=`"6`" fill=`"none`" stroke=`"$DIM`" stroke-width=`"1.2`" stroke-dasharray=`"7 5`"/>")
    [void]$s.Append((txt 436 46 12 $DIM "start" "one process"))

    [void]$s.Append((line $gather 207 380 207 $WIRE 3))
    [void]$s.Append((portbox 380 185 80 44 ":443" 8 14))
    [void]$s.Append((line 460 207 476 207 $WIRE 2))
    [void]$s.Append((srvbox 476 187 104 40 11 "TCP/WS server"))
    [void]$s.Append((line 580 207 596 207 $WIRE 2))
    [void]$s.Append("<rect x=`"596`" y=`"160`" width=`"124`" height=`"94`" rx=`"4`" fill=`"$MUXC`" stroke=`"$ARROW`" stroke-width=`"1.8`"/>")
    [void]$s.Append((txt 658 186 13 $TXT "middle" "uid -&gt;"))
    [void]$s.Append((txt 658 206 11 $ARROW "middle" "FleetPort"))
    [void]$s.Append((txt 658 222 11 $DIM "middle" "or else"))
    [void]$s.Append((txt 658 238 11 $ARROW "middle" "FleetV2Port"))

    foreach ($g in @(@{ y = 60; host = "Fleet"; mux = "FleetPort"; c1 = "Telemetry"; c2 = "Console" },
                     @{ y = 250; host = "FleetV2"; mux = "FleetV2Port"; c1 = "TelemetryV2"; c2 = "ConsoleV2" })) {
        $gy = $g.y; $cy = $gy + 70
        [void]$s.Append("<rect x=`"770`" y=`"$gy`" width=`"180`" height=`"150`" rx=`"4`" fill=`"$HOSTC`" stroke=`"$EDGE`" stroke-width=`"1.6`"/>")
        [void]$s.Append((txt 860 ($gy + 22) 14 $TXT "middle" $g.host))
        [void]$s.Append((txt 860 ($gy + 38) 10.5 $DIM "middle" "host, own generated code"))
        [void]$s.Append((connbox 786 ($gy + 52) 148 36 $g.c1 "pooled"))
        [void]$s.Append((connbox 786 ($gy + 98) 148 36 $g.c2 "pooled"))
        $mid = [int]($gy + 75)
        [void]$s.Append("<path d=`"M720 207 L745 207 L745 $mid L762 $mid`" fill=`"none`" stroke=`"$WIRE`" stroke-width=`"2`"/>")
        [void]$s.Append((hArrow1 755 770 $mid $WIRE 2))
    }

    [void]$s.Append((txt 490 448 14 $TXT "middle" "Two server generations, one port."))
    [void]$s.Append((txt 490 470 12 $DIM "middle" "Every peer host has its own uid, so a byte belongs to exactly one multiplexer - the composed function never guesses."))
    save $s $file
}

mux_one_port    "multiplex-one-port.svg"
mux_shapes      "multiplex-shapes.svg"
mux_socket      "multiplex-socket.svg"
mux_generations "multiplex-generations.svg"
