-- Normalize the supplied knight motions to the king sprite grid and ground pivot.
local root = app.params.root or '.'
local input = root .. '/Assets/Images/2D HD Character pack 1 V1.2 (1)/Spritesheets/Shadowless/1Knight/'
local output = root .. '/Assets/DivineWar/Resources/Classes/Variants/'
local directions = { 'E', 'SE', 'S', 'SW', 'W', 'NW', 'N', 'NE' }
local motions = {
  { 'MeleeSpin_Shadowless.png', 'swordsman_spin', 'Spin' },
  { 'Rolling_Shadowless.png', 'swordsman_roll', 'Roll' }
}

local source = Sprite(128, 128)
local layer = source.layers[1]
layer.name = 'Supplied knight poses'
local frameNumber = 1
for _, motion in ipairs(motions) do
  local sheet = Image{ fromFile = input .. motion[1] }
  assert(sheet.width == 1920 and sheet.height == 1024, 'Expected a 15 x 8 sheet of 128px cells')
  local packed = Image(1920, 1024)
  for row, direction in ipairs(directions) do
    local from = frameNumber
    for col = 0, 14 do
      local cell = Image(sheet, Rectangle(col * 128, (row - 1) * 128, 128, 128))
      local bounds = cell:shrinkBounds()
      local pose = Image(cell, bounds)
      local scale = 1.32
      pose:resize(math.floor(bounds.width * scale + .5), math.floor(bounds.height * scale + .5))
      local pc = app.pixelColor
      for pixel in pose:pixels() do
        local value = pixel()
        local alpha = pc.rgbaA(value)
        if alpha > 0 then
          pixel(pc.rgba(pc.rgbaR(value), math.floor(pc.rgbaG(value) * .94), math.floor(pc.rgbaB(value) * .87), alpha))
        end
      end
      local frame = frameNumber == 1 and source.frames[1] or source:newEmptyFrame()
      local art = Image(128, 128)
      art:drawImage(pose, Point(math.floor(64 + (bounds.x - 64) * scale), math.floor(112 + (bounds.y - 96) * scale)))
      source:newCel(layer, frame, art)
      packed:drawImage(art, Point(col * 128, (row - 1) * 128))
      frameNumber = frameNumber + 1
    end
    source:newTag(from, frameNumber - 1).name = motion[3] .. ' ' .. direction
  end
  packed:saveAs(output .. motion[2] .. '.png')
end
source:saveAs(root .. '/ArtSource/ClassVariants/Swordsman_Skills.aseprite')
