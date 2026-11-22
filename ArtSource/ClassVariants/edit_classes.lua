-- Executed by Aseprite. Source packs are read-only; editable derivative sprites retain layers.
local args = classVariantArgs or app.params
local root = args.root or '.'
local sources = dofile(root .. '/ArtSource/ClassVariants/sources.lua')
for key,value in pairs(dofile(root..'/ArtSource/ClassVariants/attack_sources.lua')) do sources.unarmed[key]=value end
local out = root .. '/Assets/DivineWar/Resources/Classes/Variants'
local native = root .. '/ArtSource/ClassVariants'
app.fs.makeAllDirectories(out)
local pc = app.pixelColor
local function rgba(r,g,b,a) return pc.rgba(math.floor(r),math.floor(g),math.floor(b),math.floor(a or 255)) end
local function poly(im, pts, col)
  local ymin,ymax=128,0
  for _,p in ipairs(pts) do ymin=math.min(ymin,p[2]);ymax=math.max(ymax,p[2]) end
  for y=math.max(0,math.floor(ymin)),math.min(127,math.ceil(ymax)) do
    local xs={}
    for i,p in ipairs(pts) do local q=pts[i%#pts+1]
      if (p[2]<=y and q[2]>y) or (q[2]<=y and p[2]>y) then xs[#xs+1]=p[1]+(y-p[2])*(q[1]-p[1])/(q[2]-p[2]) end
    end
    table.sort(xs)
    for i=1,#xs-1,2 do for x=math.max(0,math.ceil(xs[i])),math.min(127,math.floor(xs[i+1])) do im:drawPixel(x,y,col) end end
  end
end
local function line(im,x1,y1,x2,y2,col,w)
  local n=math.max(1,math.ceil(math.max(math.abs(x2-x1),math.abs(y2-y1))))
  for i=0,n do local x=math.floor(x1+(x2-x1)*i/n+.5);local y=math.floor(y1+(y2-y1)*i/n+.5)
    for xx=x-math.floor((w or 1)/2),x+math.floor((w or 1)/2) do for yy=y-math.floor((w or 1)/2),y+math.floor((w or 1)/2) do
      if xx>=0 and yy>=0 and xx<128 and yy<128 then im:drawPixel(xx,yy,col) end
    end end
  end
end
local function oval(im,cx,cy,rx,ry,c)
  local p={};for i=0,9 do local a=i*math.pi/5;p[#p+1]={cx+math.cos(a)*rx,cy+math.sin(a)*ry} end;poly(im,p,c)
end
local specs={
 {id='tryndamere',pack='greatsword',rgb={132,96,77},height=80,kind='barbarian'},
 {id='soraka',pack='unarmed',rgb={177,146,190},height=88,kind='healer'},
 {id='ashe',pack='longbow',rgb={69,113,151},height=88,kind='archer'},
 {id='azir',pack='unarmed',rgb={193,150,64},height=88,kind='emperor'},
 {id='evelynn',pack='unarmed',rgb={143,74,153},height=87,kind='demon',width=.88},
 {id='gragas',pack='unarmed',rgb={166,126,95},height=85,kind='brewer',width=1.32},
 {id='heimerdinger',pack='unarmed',rgb={156,117,63},height=58,kind='inventor'},
 {id='rumble',pack='unarmed',rgb={68,130,151},height=88,kind='mech'},
 {id='hecarim',pack='greatsword',rgb={72,157,145},height=80,kind='centaur'},
 {id='ivern',pack='unarmed',rgb={124,111,64},height=98,kind='tree',width=.85},
 {id='sand_soldier',pack='unarmed',rgb={186,151,79},height=83,kind='soldier'},
 {id='turret',pack='knight',rgb={121,111,91},height=40,kind='turret'},
 {id='sun_disc',pack='knight',rgb={196,153,67},height=60,kind='sun'},
 {id='daisy',pack='unarmed',rgb={135,140,125},height=95,kind='golem',width=1.3}
}
local dirMap={greatsword={6,7,8,1,2,3,4,5},unarmed={5,6,7,8,1,2,3,4},knight={1,2,3,4,5,6,7,8}}
local images={};local normalized={}
local function load(path)
  if not images[path] then images[path]=Image{fromFile=path:match('^Assets/') and root..'/'..path or path} end
  return images[path]
end
local function original(pack,motion,dir,frame)
  local source=sources[pack][motion][dirMap[pack][dir+1]]
  local rect=source.rects[math.min(#source.rects,math.floor(frame*#source.rects/15)+1)]
  return Image(load(source.path),Rectangle(rect.x,rect.y,rect.w,rect.h))
end
local bowDirections={'E','SE','S','SW','W','NW','N','NE'}
local bowAngles={0,315,270,225,180,135,90,45}
local bowMotions={idle='Idle',walk='Walk',attack='QuickShot'}
local function longbowFrame(motion,dir,frame)
  local action=bowMotions[motion]
  local path=string.format('Assets/Images/2D HD Character pack 1 V1.2 (1)/9Longbow/%s/%s/%s_%d_%03d.png',
    action,bowDirections[dir+1],action,bowAngles[dir+1],frame*2+1)
  local source=Image(load(path))
  -- Remove the baked ground shadow and keep one scale and ground point across all poses.
  for pixel in source:pixels() do if pc.rgbaA(pixel())<180 then pixel(0) end end
  local bounds=source:shrinkBounds()
  local pose=Image(source,bounds)
  local scale=1.5
  pose:resize(math.floor(bounds.width*scale+.5),math.floor(bounds.height*scale+.5))
  local result=Image(128,128)
  result:drawImage(pose,Point(math.floor(64+(bounds.x-64)*scale+.5),math.floor(112+(bounds.y-100)*scale+.5)))
  return result
end
local function baseFrame(spec,motion,dir,frame)
  if spec.pack=='longbow' then return longbowFrame(motion,dir,frame) end
  local key=spec.id..'_'..dir
  local guide=normalized[key]
  if not guide then
    local idle=original(spec.pack,'idle',dir,0)
    for it in idle:pixels() do if pc.rgbaA(it())<120 then it(0) end end
    local b=idle:shrinkBounds()
    if spec.pack=='greatsword' then
      local x1,y1,x2,y2=idle.width,idle.height,0,0
      for p in idle:pixels() do local q=p();if pc.rgbaA(q)>0 and pc.rgbaB(q)>pc.rgbaR(q)*1.08 and pc.rgbaG(q)>pc.rgbaR(q)*.92 then
        x1=math.min(x1,p.x);y1=math.min(y1,p.y);x2=math.max(x2,p.x);y2=math.max(y2,p.y)
      end end
      if y2>y1 then b=Rectangle(x1,y1,x2-x1+1,y2-y1+1) end
    end
    guide={cx=b.x+b.width/2,bottom=b.y+b.height,scale=spec.height/b.height};normalized[key]=guide
  end
  local pose=motion
  if motion=='attack' then
    if spec.kind=='archer' then pose='idle'
    elseif spec.kind=='demon' or spec.kind=='brewer' or spec.kind=='mech' or spec.kind=='golem' then pose='slash'
    elseif spec.kind=='soldier' then pose='backslash' end
  end
  local raw=original(spec.pack,pose,dir,frame)
  for it in raw:pixels() do if pc.rgbaA(it())<120 then it(0) end end
  local bounds=raw:shrinkBounds();local img=Image(raw,bounds)
  local sx=guide.scale*(spec.width or 1)
  img:resize(math.max(1,math.floor(img.width*sx+.5)),math.max(1,math.floor(img.height*guide.scale+.5)))
  local result=Image(128,128)
  local floor=112
  result:drawImage(img,Point(math.floor(64+(bounds.x-guide.cx)*sx+.5),math.floor(floor+(bounds.y-guide.bottom)*guide.scale+.5)))
  return result
end
local gold=rgba(201,166,80);local goldDark=rgba(126,94,42);local steel=rgba(170,186,180);local wood=rgba(94,65,43)
local function decorate(spec,body,back,gear,dir,frame,motion)
  local k=spec.kind;local a=dir*math.pi/4;local fx,fy=math.cos(a),math.sin(a)*.5
  local bob=motion=='walk' and math.floor(math.sin(frame/15*math.pi*2)*1.5) or 0
  local y=bob;local wx,wy=64+fx*23,66+fy*17+y
  local front=dir<4 or dir==7
  local function staff(crescent)
    line(gear,wx,29+y,wx-2,108,goldDark,3);line(gear,wx,30+y,wx-2,105,gold,1)
    if crescent then poly(gear,{{wx-1,16+y},{wx-10,24+y},{wx-9,35+y},{wx,39+y},{wx+8,32+y},{wx,33+y},{wx-4,26+y}},gold)
    else poly(gear,{{wx,17+y},{wx-6,31+y},{wx,42+y},{wx+6,31+y}},gold) end
  end
  if k=='barbarian' then
    poly(back,{{46,42+y},{74,42+y},{84,97},{62,105},{43,89}},rgba(96,38,36))
    poly(gear,{{45,45+y},{54,39+y},{64,44+y},{75,38+y},{84,47+y},{73,54+y},{55,51+y}},rgba(113,96,69))
    line(gear,57,80+y,75,77+y,rgba(151,51,39),4)
  elseif k=='healer' then
    poly(back,{{48,43+y},{78,43+y},{87,105},{72,113},{43,106}},rgba(57,111,116))
    poly(gear,{{51,64+y},{73,64+y},{82,107},{65,112},{47,106}},rgba(206,204,171))
    poly(gear,{{51,64+y},{59,69+y},{59,108},{47,106}},rgba(151,153,133))
    line(gear,64,70+y,67,108,gold,2)
    oval(gear,62,34+y,12,9,rgba(223,225,218));poly(gear,{{57,25+y},{61,12+y},{65,26+y}},gold)
    staff(true)
  elseif k=='archer' then
    poly(back,{{49,39+y},{77,39+y},{86,104},{63,110},{42,98}},rgba(27,53,80))
    poly(gear,{{48,36+y},{52,24+y},{66,20+y},{78,29+y},{78,44+y},{69,37+y},{59,36+y},{50,45+y}},rgba(44,76,109))
    if front then line(gear,57,38+y,55,63+y,rgba(215,222,219),3) end
    local side=fx>=0 and 1 or -1;local bx=64+side*25
    line(gear,bx,43+y,bx+side*9,60+y,rgba(112,203,220),3);line(gear,bx+side*9,60+y,bx+side*9,78+y,rgba(162,225,232),3);line(gear,bx+side*9,78+y,bx,96+y,rgba(112,203,220),3)
    line(gear,bx,43+y,bx,96+y,rgba(217,239,237),1);line(gear,bx-12*side,70+y,bx+17*side,70+y,steel,1)
  elseif k=='emperor' or k=='soldier' then
    poly(back,{{47,46+y},{75,45+y},{84,102},{65,111},{43,98}},rgba(105,44,49))
    staff(false)
    if k=='emperor' then
      poly(gear,{{53,36+y},{53,25+y},{60,16+y},{70,19+y},{75,32+y},{66,44+y}},gold)
      poly(gear,{{66,31+y},{82,34+y},{69,39+y}},goldDark)
      poly(gear,{{51,24+y},{47,13+y},{58,19+y},{65,9+y},{69,23+y}},gold)
    end
  elseif k=='demon' then
    for sign=-1,1,2 do
      local x=64+sign*16
      line(back,x,58+y,x+sign*17,74+y,rgba(91,42,105),4);line(back,x+sign*17,74+y,x+sign*25,50+y,rgba(115,49,128),4)
      poly(back,{{x+sign*25,50+y},{x+sign*21,33+y},{x+sign*31,42+y}},rgba(186,104,181))
    end
    oval(gear,63,30+y,12,9,rgba(112,43,103));poly(gear,{{52,34+y},{48,19+y},{57,27+y}},rgba(172,107,162));poly(gear,{{70,28+y},{79,20+y},{75,36+y}},rgba(172,107,162))
  elseif k=='brewer' then
    oval(gear,63,71+y,25,27,rgba(183,145,109));poly(gear,{{42,63+y},{53,53+y},{61,59+y},{60,88+y},{47,90+y}},rgba(207,164,119))
    poly(gear,{{51,37+y},{73,37+y},{77,50+y},{65,63+y},{52,51+y}},rgba(137,65,38))
    local bx=64+fx*20;oval(gear,bx,82+y,16,21,wood);poly(gear,{{bx-10,65+y},{bx+8,63+y},{bx+12,99+y},{bx-9,101+y}},rgba(137,96,52))
    line(gear,bx-13,70+y,bx+13,69+y,rgba(89,89,80),3);line(gear,bx-13,94+y,bx+13,95+y,rgba(89,89,80),3);line(gear,bx,65+y,bx,100+y,wood,1)
  elseif k=='inventor' then
    poly(gear,{{47,66+y},{44,53+y},{52,54+y},{55,44+y},{63,48+y},{70,43+y},{74,54+y},{84,54+y},{80,67+y},{69,73+y},{55,70+y}},rgba(205,167,69))
    poly(gear,{{50,58+y},{57,48+y},{64,54+y},{72,48+y},{78,57+y},{66,62+y}},rgba(235,198,88))
    oval(gear,57,69+y,6,5,wood);oval(gear,71,69+y,6,5,wood);oval(gear,57,69+y,4,3,rgba(109,185,193));oval(gear,71,69+y,4,3,rgba(109,185,193))
    line(gear,86,79+y,83,106,steel,4);poly(gear,{{81,78+y},{78,68+y},{82,65+y},{85,73+y},{90,70+y},{90,64+y},{95,69+y},{93,78+y}},steel)
  elseif k=='mech' then
    poly(back,{{43,66+y},{39,51+y},{48,43+y},{79,44+y},{89,58+y},{84,79+y}},wood)
    poly(gear,{{39,68+y},{48,59+y},{80,59+y},{91,72+y},{86,94+y},{43,94+y}},rgba(147,98,53))
    poly(gear,{{39,68+y},{53,63+y},{55,88+y},{43,94+y}},rgba(186,132,62));poly(gear,{{55,66+y},{77,65+y},{78,84+y},{57,87+y}},rgba(92,91,76))
    for sign=-1,1,2 do local lx=64+sign*16;line(gear,lx,92+y,lx+sign*5,108,rgba(86,85,74),7);poly(gear,{{lx+sign*5-10,106},{lx+sign*5+9,106},{lx+sign*5+11,114},{lx+sign*5-12,114}},rgba(130,116,81)) end
    local gun=fx>=0 and 1 or -1;line(gear,64+gun*25,74+y,64+gun*43,82+y,rgba(71,78,74),9);line(gear,64+gun*25,72+y,64+gun*40,79+y,rgba(167,142,88),3)
    line(back,42,54+y,38,31+y,rgba(89,93,79),7)
  elseif k=='centaur' then
    local horse=load(root..'/Assets/DivineWar/Art/horse.png');local hw=horse.width/4;local hh=horse.height/2
    local part=Image(horse,Rectangle((dir%4)*hw,math.floor(dir/4)*hh,hw,hh));local b=part:shrinkBounds();part=Image(part,b);part:resize(math.floor(b.width*64/b.height+.5),64)
    for it in part:pixels() do local px=it();if pc.rgbaA(px)>100 then local l=(pc.rgbaR(px)+pc.rgbaG(px)+pc.rgbaB(px))/765;it(rgba(35+65*l,79+125*l,84+113*l)) else it(0) end end
    for yy=73,127 do for xx=0,127 do body:drawPixel(xx,yy,0) end end
    back:drawImage(part,Point(64-math.floor(part.width/2),49));line(gear,wx,32+y,wx-5,109,rgba(79,146,140),3)
    poly(gear,{{wx,25+y},{wx+12,37+y},{wx+8,51+y},{wx,44+y},{wx-6,40+y}},rgba(106,192,182))
  elseif k=='tree' then
    for sign=-1,1,2 do line(back,64+sign*13,50+y,64+sign*31,39+y,wood,5);line(back,64+sign*31,39+y,64+sign*28,20+y,wood,4);line(back,64+sign*28,28+y,64+sign*40,21+y,wood,3) end
    oval(gear,48,41+y,15,11,rgba(75,105,48));oval(gear,75,39+y,16,12,rgba(97,131,54));oval(gear,64,25+y,14,10,rgba(110,137,62))
    poly(gear,{{57,31+y},{68,30+y},{74,39+y},{68,51+y},{57,46+y},{54,38+y}},rgba(132,111,67))
    if front then line(gear,58,38+y,61,37+y,rgba(218,202,126),2);line(gear,67,37+y,70,38+y,rgba(218,202,126),2) end
    poly(gear,{{54,48+y},{69,46+y},{74,62+y},{63,77+y},{54,64+y}},rgba(83,119,51))
    line(gear,61,79+y,58,106,wood,2);line(gear,73,78+y,76,108,wood,2)
  elseif k=='turret' or k=='sun' then
    poly(back,{{36,104},{50,84},{78,84},{93,104},{76,113},{49,112}},rgba(114,97,65))
    if k=='turret' then
      for sign=-1,1,2 do line(gear,64+sign*12,88,64+sign*27,110,wood,6) end
      oval(gear,64,76,22,17,rgba(143,113,57));oval(gear,64,77,12,10,rgba(62,108,118));oval(gear,64,77,7,7,rgba(115,196,205))
      line(gear,64+fx*7,73+fy*6,64+fx*37,73+fy*26,rgba(83,87,78),11);line(gear,64+fx*8,70+fy*6,64+fx*34,70+fy*26,steel,3)
    else
      poly(gear,{{52,102},{54,60},{73,60},{77,102}},rgba(176,153,100));poly(gear,{{52,102},{54,60},{60,63},{60,105}},rgba(125,105,68))
      oval(gear,64,46,27,26,goldDark);oval(gear,62,44,23,23,gold)
      for j=0,7 do local ang=j*math.pi/4;local cx=64+math.cos(ang)*29;local cy=45+math.sin(ang)*29;poly(gear,{{cx-5,cy},{cx,cy-8},{cx+5,cy+2},{cx,cy+5}},gold) end
      poly(gear,{{45,44},{60,27},{64,47},{55,59}},rgba(227,191,99))
    end
  elseif k=='golem' then
    poly(gear,{{42,57+y},{52,43+y},{77,44+y},{89,62+y},{88,85+y},{74,101},{49,99},{36,79+y}},rgba(133,139,122))
    poly(gear,{{42,57+y},{52,43+y},{62,53+y},{57,81+y},{41,85+y},{36,79+y}},rgba(101,110,98))
    oval(gear,37,77+y,13,17,rgba(144,148,129));oval(gear,89,78+y,13,17,rgba(111,120,108))
    poly(gear,{{48,43+y},{58,35+y},{76,37+y},{80,50+y},{70,60+y},{51,56+y}},rgba(102,113,98))
    oval(gear,50,48+y,12,6,rgba(85,117,54));oval(gear,78,50+y,13,6,rgba(102,133,58))
    if front then oval(gear,60,50+y,2,3,rgba(232,205,100));oval(gear,70,50+y,2,3,rgba(232,205,100)) end
    line(gear,77,40+y,81,27+y,rgba(83,111,57),2);oval(gear,82,25+y,6,4,rgba(215,205,162));oval(gear,82,25+y,2,2,gold)
  end
end
local fitted=dofile(root..'/ArtSource/ClassVariants/fit_outfits.lua')
local exports={}
for _,spec in ipairs(specs) do
  if not args.only or args.only==spec.id then
    local spr=Sprite(128,128,ColorMode.RGB);spr.layers[1].name='Imported source - recolored'
    local rear=spr:newLayer();rear.name='Added silhouette - rear';rear.stackIndex=1
    local base=spr.layers[2];local gear=spr:newLayer();gear.name='Added equipment and class details'
    local frameIndex=0
    for mi,motion in ipairs({'idle','walk','attack'}) do
      local sheet=Image(1920,1024)
      for dir=0,7 do
        local tagStart=frameIndex+1
        for frame=0,14 do
          frameIndex=frameIndex+1;if frameIndex>1 then spr:newEmptyFrame() end
          local body=baseFrame(spec,motion,dir,frame);local back=Image(128,128);local equip=Image(128,128)
          if spec.pack~='longbow' and not fitted(spec,body,back,equip,dir,frame,motion,{line=line,poly=poly,oval=oval,rgba=rgba,load=load,root=root}) then
            decorate(spec,body,back,equip,dir,frame,motion)
          end
          spr:newCel(rear,frameIndex,back);spr:newCel(base,frameIndex,body);spr:newCel(gear,frameIndex,equip)
          spr.frames[frameIndex].duration=motion=='idle' and .08 or motion=='walk' and .04 or .55/15
          local flat=Image(128,128);flat:drawImage(back);flat:drawImage(body);flat:drawImage(equip)
          sheet:drawImage(flat,Point(frame*128,dir*128))
        end
        local tag=spr:newTag(tagStart,frameIndex);tag.name=motion..'_'..({'E','SE','S','SW','W','NW','N','NE'})[dir+1]
      end
      sheet:saveAs(out..'/'..spec.id..'_'..motion..'.png')
    end
    spr:saveAs(native..'/'..spec.id..'.aseprite');exports[#exports+1]=spec.id;spr:close()
  end
end
return {exported=exports,cell=128,framesPerDirection=15,directions=8}
