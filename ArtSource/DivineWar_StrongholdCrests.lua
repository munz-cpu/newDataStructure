-- Run with Aseprite --batch --script this-file. Artwork is drawn in Aseprite.
local destination = app.params["destination"] or "."
local function rgba(r,g,b) return app.pixelColor.rgba(r,g,b,255) end
local outline = rgba(29,24,22)
local rimLight, rimDark = rgba(223,188,107), rgba(123,86,44)
local function make(name, light, shade, enemy)
  local sprite = Sprite(24,24,ColorMode.RGB)
  sprite.layers[1].name = "Faceted shield"
  local image = sprite.cels[1].image
  -- Flat broad planes and stepped edges keep the silhouette legible at HUD size.
  for y=2,21 do
    local inset = y<5 and 3 or y<14 and 2 or 2+math.floor((y-13)*1.15)
    for x=inset,23-inset do
      local edge = x==inset or x==23-inset or y==2 or y==21
      local rim = x<=inset+2 or x>=21-inset or y<=4
      image:drawPixel(x,y,edge and outline or rim and (x<12 and rimLight or rimDark) or (x<12 and light or shade))
    end
  end
  local layer=sprite:newLayer(); layer.name=enemy and "Raider crossed blades" or "Keep and battlements"
  local crest=Image(24,24,ColorMode.RGB)
  local ivory=rgba(255,236,181)
  local shadow=rgba(58,43,30)
  local function pixel(x,y,c) crest:drawPixel(x,y,c) end
  if enemy then
    for n=0,7 do
      for w=0,1 do pixel(8+n,7+n+w,shadow); pixel(15-n,7+n+w,shadow) end
      pixel(8+n,7+n,ivory); pixel(15-n,7+n,ivory)
    end
    for x=7,10 do pixel(x,14,rimLight) end
    for x=13,16 do pixel(x,14,rimLight) end
  else
    for y=8,15 do for x=8,15 do pixel(x,y,shadow) end end
    for y=8,14 do for x=9,14 do pixel(x,y,ivory) end end
    for x=8,15 do if x==8 or x==9 or x==11 or x==12 or x==14 or x==15 then pixel(x,7,ivory) end end
    for y=12,15 do pixel(11,y,shadow);pixel(12,y,shadow) end
  end
  sprite:newCel(layer,1,crest)
  sprite:saveAs(destination.."/ArtSource/"..name..".aseprite")
  sprite:saveCopyAs(destination.."/Assets/DivineWar/Resources/Minimap/"..name..".png")
  sprite:close()
end
make("allied-stronghold",rgba(48,137,128),rgba(29,81,79),false)
make("enemy-stronghold",rgba(172,64,48),rgba(103,39,35),true)
