-- Native faceted sprite authoring. Building frames follow the existing E, SE, S,
-- SW, W, NW, N, NE order and share the ground contact at (80,130).
local root = app.params.root or '.'
local source = Sprite{fromFile=root..'/ArtSource/DivineWar_Buildings_LowPoly_8Dir.aseprite'}
local s = Sprite(160,160,ColorMode.RGB)
s.layers[1].name='Faceted architecture'
local architecture=s.layers[1]
local details=s:newLayer(); details.name='Weapons and mechanisms'
local W=160
local function rgba(r,g,b,a) return app.pixelColor.rgba(r,g,b,a or 255) end
local function poly(img, pts, c)
  local ymin,ymax=159,0
  for _,p in ipairs(pts) do ymin=math.min(ymin,p[2]); ymax=math.max(ymax,p[2]) end
  for y=math.max(0,math.floor(ymin)),math.min(159,math.ceil(ymax)) do
    local xs={}
    for i,a in ipairs(pts) do
      local b=pts[i%#pts+1]
      if (a[2]<=y+.5 and b[2]>y+.5) or (b[2]<=y+.5 and a[2]>y+.5) then
        xs[#xs+1]=a[1]+(y+.5-a[2])*(b[1]-a[1])/(b[2]-a[2])
      end
    end
    table.sort(xs)
    for k=1,#xs-1,2 do for x=math.max(0,math.ceil(xs[k])),math.min(159,math.floor(xs[k+1])) do img:drawPixel(x,y,c) end end
  end
end
local function line(img,x,y,xx,yy,c,width)
  local n=math.max(math.abs(xx-x),math.abs(yy-y),1)
  for i=0,n do
    local px=math.floor(x+(xx-x)*i/n); local py=math.floor(y+(yy-y)*i/n)
    for dx=0,(width or 1)-1 do for dy=0,(width or 1)-1 do
      if px+dx>=0 and px+dx<160 and py+dy>=0 and py+dy<160 then img:drawPixel(px+dx,py+dy,c) end
    end end
  end
end
local names={'bruiser_school','archer_school','archer_tower','mine','jump_pad'}
local dirs={'E','SE','S','SW','W','NW','N','NE'}
local wood=rgba(112,77,43); local lightwood=rgba(157,115,66); local darkwood=rgba(69,46,29)
local steel=rgba(155,164,155); local steelDark=rgba(77,89,86); local gold=rgba(210,172,94)
local outline=rgba(40,37,29)
local sheet=Image(160*8,160*10,ColorMode.RGB)
for kind=1,5 do for team=0,1 do for dir=0,7 do
  local index=(kind-1)*16+team*8+dir+1
  if index>1 then s:newEmptyFrame() end
  local img=Image(160,160,ColorMode.RGB); local extra=Image(160,160,ColorMode.RGB)
  local teal=team==0 and rgba(98,142,129) or rgba(155,99,77)
  local tealLight=team==0 and rgba(137,177,154) or rgba(189,133,100)
  local a=dir*math.pi/4
  local function point(x,y,z)
    local rx=x*math.cos(a)-y*math.sin(a); local ry=x*math.sin(a)+y*math.cos(a)
    return {80+rx*.87-ry*.5,124+rx*.24+ry*.43-z*(kind==3 and .82 or 1)}
  end
  local faces={}
  local function face(vertices,c)
    local pts={}; local depth=0
    for _,v in ipairs(vertices) do pts[#pts+1]=point(v[1],v[2],v[3]); depth=depth+pts[#pts][2]+v[3]*.98 end
    faces[#faces+1]={pts=pts,c=c,depth=depth/#vertices}
  end
  local function box(x,y,z,w,d,h,colors)
    local p={{x,y,z},{x+w,y,z},{x+w,y+d,z},{x,y+d,z},{x,y,z+h},{x+w,y,z+h},{x+w,y+d,z+h},{x,y+d,z+h}}
    local order={{1,2,6,5},{2,3,7,6},{3,4,8,7},{4,1,5,8},{5,6,7,8}}
    for i,ids in ipairs(order) do
      local ps={}; for _,v in ipairs(ids) do ps[#ps+1]=p[v] end
      -- Back walls are hidden by nearer surfaces; top remains the lightest face.
      face(ps,colors[i] or colors[#colors])
    end
  end
  if kind<=2 then
    local cel=source.layers[2]:cel(17+team*24+dir)
    img:drawImage(cel.image,Point(cel.position.x+25,cel.position.y+60))
    -- A broad sign at the front stays legible at game scale.
    poly(extra,{{58,104},{88,98},{91,119},{61,126}},outline)
    poly(extra,{{60,105},{86,100},{88,117},{63,123}},darkwood)
    if kind==1 then
      line(extra,65,108,81,116,steel,3); line(extra,80,104,66,120,steel,3)
      line(extra,63,115,69,107,gold,2); line(extra,77,117,83,108,gold,2)
      poly(extra,{{109,111},{118,114},{117,132},{108,129}},wood)
      line(extra,113,105,113,135,lightwood,3); line(extra,105,115,121,121,lightwood,3)
    else
      poly(extra,{{70,104},{82,107},{85,113},{80,119},{69,120},{64,113}},gold)
      poly(extra,{{71,107},{79,109},{81,113},{78,117},{71,117},{68,113}},teal)
      poly(extra,{{73,110},{77,111},{78,114},{74,116},{71,113}},rgba(166,72,49))
      line(extra,73,113,90,103,steel,1)
      poly(extra,{{105,112},{116,108},{122,114},{120,127},{111,133},{104,127}},gold)
      poly(extra,{{109,115},{116,113},{119,118},{117,125},{112,129},{108,125}},teal)
      poly(extra,{{112,117},{116,117},{117,122},{113,126},{110,122}},rgba(160,65,43))
    end
  elseif kind==3 then
    box(-28,-25,0,56,50,9,{rgba(100,99,77),rgba(83,83,66),rgba(125,123,94),rgba(108,108,84),rgba(155,151,116)})
    for _,p in ipairs({{-21,-18},{15,-18},{-21,12},{15,12}}) do box(p[1],p[2],9,7,7,75,{wood,darkwood,lightwood,wood,lightwood}) end
    box(-24,-21,76,48,42,7,{wood,darkwood,lightwood,wood,rgba(177,137,80)})
    box(-25,-23,83,50,5,17,{wood,darkwood,lightwood,wood,lightwood})
    box(-25,18,83,50,5,17,{wood,darkwood,lightwood,wood,lightwood})
    box(-25,-18,83,5,36,17,{wood,darkwood,lightwood,wood,lightwood})
    box(20,-18,83,5,36,17,{wood,darkwood,lightwood,wood,lightwood})
    -- Faceted canvas canopy, raised above the parapet.
    face({{-30,-27,104},{0,-27,128},{0,27,128},{-30,27,104}},tealLight)
    face({{0,-27,128},{30,-27,104},{30,27,104},{0,27,128}},teal)
    face({{-30,27,104},{30,27,104},{0,27,128}},team==0 and rgba(68,107,100) or rgba(118,72,58))
    for _,p in ipairs({{-22,-19},{20,-19},{-22,17},{20,17}}) do box(p[1],p[2],95,3,3,13,{wood,darkwood,lightwood}) end
    -- Ladder along the near side is drawn after the structural faces below.
  else
    local r=kind==4 and 23 or 34
    local top={}; local bottom={}
    for i=0,7 do
      local t=i*math.pi/4; top[i+1]=point(math.cos(t)*r,math.sin(t)*r,kind==4 and 7 or 9)
      bottom[i+1]=point(math.cos(t)*r,math.sin(t)*r,0)
    end
    for i=1,8 do
      local j=i%8+1
      poly(img,{top[i],top[j],bottom[j],bottom[i]},kind==4 and steelDark or darkwood)
    end
    poly(img,top,kind==4 and steel or lightwood)
    if kind==4 then
      local ring={}; for i=0,7 do ring[i+1]=point(math.cos(i*math.pi/4)*14,math.sin(i*math.pi/4)*14,9) end
      poly(img,ring,wood)
      local trigger={}; for i=0,5 do trigger[i+1]=point(math.cos(i*math.pi/3)*8,math.sin(i*math.pi/3)*8,12) end
      poly(img,trigger,rgba(171,71,45))
      for i=0,3 do local p=point(math.cos(i*math.pi/2)*19,math.sin(i*math.pi/2)*19,8); line(img,p[1],p[2],p[1]+2,p[2],gold,2) end
    else
      local inner={}; for i=0,7 do inner[i+1]=point(math.cos(i*math.pi/4)*25,math.sin(i*math.pi/4)*25,11) end
      poly(img,inner,teal)
      local arrow={{-15,-6,13},{2,-6,13},{2,-15,13},{18,0,13},{2,15,13},{2,6,13},{-15,6,13}}
      local pp={}; for _,v in ipairs(arrow) do pp[#pp+1]=point(v[1],v[2],v[3]) end
      poly(img,pp,gold)
      for _,x in ipairs({-25,25}) do local p=point(x,0,13); line(img,p[1]-2,p[2]-2,p[1]+2,p[2]+2,steel,2) end
    end
  end
  if kind==3 then
    table.sort(faces,function(x,y) return x.depth<y.depth end)
    for _,f in ipairs(faces) do poly(img,f.pts,f.c) end
    local p=point(-7,24,12); local q=point(-7,24,77)
    local pp=point(7,24,12); local qq=point(7,24,77)
    line(extra,p[1],p[2],q[1],q[2],lightwood,2); line(extra,pp[1],pp[2],qq[1],qq[2],wood,2)
    for z=18,72,9 do local l=point(-7,24,z); local r=point(7,24,z); line(extra,l[1],l[2],r[1],r[2],lightwood,2) end
    local h=point(0,21,109)
    poly(extra,{{h[1]-4,h[2]-7},{h[1]+2,h[2]-8},{h[1]+5,h[2]-3},{h[1]+3,h[2]+2},{h[1]-4,h[2]+1}},rgba(191,155,113))
    line(extra,h[1]-5,h[2]+2,h[1]+3,h[2]+7,teal,4)
    line(extra,h[1]+7,h[2]-3,h[1]+10,h[2]+3,gold,2); line(extra,h[1]+10,h[2]+3,h[1]+7,h[2]+9,gold,2)
    line(extra,h[1]+7,h[2]-3,h[1]+7,h[2]+9,steel,1)
  end
  s:newCel(architecture,index,img,Point(0,0)); s:newCel(details,index,extra,Point(0,0))
  local tag=s:newTag(index,index); tag.name=names[kind]..'_'..(team==0 and 'teal_' or 'red_')..dirs[dir+1]
  local merged=Image(160,160,ColorMode.RGB); merged:drawImage(img); merged:drawImage(extra)
  sheet:drawImage(merged,Point(dir*160,((kind-1)*2+team)*160))
end end end
local sl=s:newSlice(Rectangle(0,0,160,160)); sl.name='Ground contact'; sl.pivot=Point(80,130)
s:saveAs(root..'/ArtSource/DivineWar_Facilities_8Dir.aseprite')
sheet:saveAs(root..'/Assets/DivineWar/Resources/Buildings/facilities.png')
print('Authored 80 facility sprites with layered native source.')
local humans=Sprite{fromFile=root..'/ArtSource/Human_Gnome_LowPoly_8Dir.aseprite'}
local units=Sprite(80,80,ColorMode.RGB)
units.layers[1].name='Original faceted human'
local gear=units:newLayer(); gear.name='Class armor and weapons'
local unitSheet=Image(640,320,ColorMode.RGB)
for class=0,1 do for team=0,1 do for dir=0,7 do
  local index=class*16+team*8+dir+1
  if index>1 then units:newEmptyFrame() end
  local body=Image(80,80,ColorMode.RGB)
  local c=humans.layers[2]:cel(team*8+dir+1)
  body:drawImage(c.image,c.position)
  local weapon=Image(80,80,ColorMode.RGB)
  local sign=(dir>=3 and dir<=5) and -1 or 1
  local xx=34+sign*13
  if class==0 then
    poly(weapon,{{26,32},{34,29},{42,32},{41,43},{34,46},{27,42}},steelDark)
    poly(weapon,{{27,33},{33,31},{33,43},{28,41}},steel)
    poly(weapon,{{xx-2,40},{xx+2,40},{xx+4,18},{xx,13},{xx-3,20}},steel)
    line(weapon,xx-5,39,xx+5,39,gold,2); line(weapon,xx,41,xx,47,wood,2)
  else
    poly(weapon,{{27,32},{34,30},{41,32},{40,42},{28,42}},wood)
    poly(weapon,{{28,33},{34,31},{34,41},{29,41}},lightwood)
    line(weapon,xx,25,xx+sign*5,32,gold,2)
    line(weapon,xx+sign*5,32,xx+sign*5,42,wood,2)
    line(weapon,xx+sign*5,42,xx,49,gold,2)
    line(weapon,xx,25,xx,49,steel,1)
    line(weapon,xx-sign*6,37,xx+sign*8,37,steel,1)
    line(weapon,27,29,24,20,wood,3)
  end
  units:newCel(units.layers[1],index,body); units:newCel(gear,index,weapon)
  local tag=units:newTag(index,index); tag.name=(class==0 and 'bruiser_' or 'archer_')..(team==0 and 'teal_' or 'red_')..dirs[dir+1]
  local merged=Image(80,80,ColorMode.RGB); merged:drawImage(body); merged:drawImage(weapon)
  unitSheet:drawImage(merged,Point(dir*80,(class*2+team)*80))
end end end
units:saveAs(root..'/ArtSource/DivineWar_Specialists_8Dir.aseprite')
unitSheet:saveAs(root..'/Assets/DivineWar/Resources/Buildings/specialists.png')
