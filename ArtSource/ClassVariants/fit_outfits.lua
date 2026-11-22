-- Fitted material editing: all clothing follows opaque source pixels, never a fixed front panel.
local pc=app.pixelColor
return function(spec,body,back,gear,dir,frame,motion,draw)
  if spec.kind=='turret' or spec.kind=='sun' then return false end
  local line,poly,oval,c=draw.line,draw.poly,draw.oval,draw.rgba
  local b=body:shrinkBounds();local minx,miny,maxx,maxy=128,128,0,0
  for p in body:pixels() do local q=p();local r,g,bb=pc.rgbaR(q),pc.rgbaG(q),pc.rgbaB(q)
    if pc.rgbaA(q)>0 and bb>r*1.08 and g>r*.92 then minx=math.min(minx,p.x);maxx=math.max(maxx,p.x);miny=math.min(miny,p.y);maxy=math.max(maxy,p.y) end
  end
  if maxy<=miny then minx,miny,maxx,maxy=b.x,b.y,b.x+b.width-1,b.y+b.height-1 end
  -- Track the dense head silhouette near its idle height, excluding raised hands/blades.
  local h=spec.height;maxy=112
  local expected=112-h;local bestScore=-1;local headCenter=64;local centerY=expected+h*.13
  for y=math.max(0,math.floor(expected-6)),math.min(127,math.floor(expected+h*.23)) do
    local run=0;local start=40
    for x=40,89 do
      if x<89 and pc.rgbaA(body:getPixel(x,y))>0 then if run==0 then start=x end;run=run+1
      else
        local score=run-math.abs(y-(expected+h*.13))*.32
        if run>=4 and score>bestScore then bestScore=score;headCenter=(start+x-1)/2;centerY=y end
        run=0
      end
    end
  end
  local cx=headCenter;local headTop=centerY-h*.13;local headBottom=headTop+h*.265
  miny=headTop
  local headY=(headTop+headBottom)/2;local chestY=miny+h*.43;local waist=miny+h*.65
  local half=math.max(10,h*.155);local k=spec.kind
  local fx,fy=math.cos(dir*math.pi/4),math.sin(dir*math.pi/4)*.45
  local front=dir<=3 or dir==7
  local palettes={
    barbarian={torso={94,104,109},cloth={132,40,35},skin={189,143,108},hair={75,51,35}},
    healer={torso={216,211,183},cloth={54,119,115},skin={164,126,179},hair={225,224,214}},
    archer={torso={71,111,145},cloth={35,62,91},skin={199,174,151},hair={216,224,224}},
    emperor={torso={199,158,71},cloth={114,47,53},skin={105,82,54},hair={199,158,71}},
    demon={torso={75,43,91},cloth={114,55,132},skin={170,108,177},hair={143,48,110}},
    brewer={torso={180,135,99},cloth={85,64,46},skin={197,148,108},hair={149,67,35}},
    inventor={torso={141,114,71},cloth={69,83,78},skin={184,152,110},hair={229,186,69}},
    mech={torso={160,112,65},cloth={77,87,91},skin={80,130,150},hair={66,80,86}},
    centaur={torso={83,150,145},cloth={43,83,89},skin={80,150,145},hair={97,181,167}},
    tree={torso={136,113,73},cloth={94,112,57},skin={153,128,79},hair={84,123,52}},
    soldier={torso={183,150,82},cloth={118,58,49},skin={98,84,60},hair={183,150,82}},
    golem={torso={153,158,144},cloth={111,122,104},skin={153,158,144},hair={93,124,60}}
  }
  local pal=palettes[k] or palettes.barbarian
  local original=Image(body)
  local function shade(rgb,q,extra)
    local lum=(pc.rgbaR(q)*.3+pc.rgbaG(q)*.59+pc.rgbaB(q)*.11)/255
    local s=.52+lum*.72+(extra or 0)
    return c(math.min(255,rgb[1]*s),math.min(255,rgb[2]*s),math.min(255,rgb[3]*s))
  end
  local function covered(x,y) return x>=0 and x<128 and y>=0 and y<128 and pc.rgbaA(original:getPixel(x,y))>0 end
  local function fittedDot(x,y,rgb)
    x,y=math.floor(x),math.floor(y);if covered(x,y) then gear:drawPixel(x,y,c(rgb[1],rgb[2],rgb[3])) end
  end
  -- Repaint surfaces rather than covering the moving figure with independent geometry.
  for p in body:pixels() do local q=p();if pc.rgbaA(q)>0 then
    local x,y=p.x,p.y;local rgb=pal.torso
    if y>waist then rgb=pal.cloth end
    if y>maxy-h*.12 then rgb=(k=='tree' or k=='golem') and pal.torso or {57,57,51} end
    local arm=math.abs(x-cx)>half and y>headBottom and y<waist+6
    if arm and (k=='barbarian' or k=='brewer' or k=='healer' or k=='demon') then rgb=pal.skin end
    if y<=headBottom then
      rgb=pal.skin
      if y<headTop+h*.085 or not front then rgb=pal.hair end
      if k=='archer' or k=='emperor' or k=='soldier' or k=='mech' then rgb=pal.cloth end
      if k=='emperor' or k=='soldier' then rgb=pal.torso end
    end
    if k=='brewer' and y>chestY-3 and y<waist+5 and math.abs(x-cx)<half then rgb=pal.skin end
    if k=='healer' and y>headBottom and math.abs(x-cx)<half and y<maxy-h*.13 then rgb=pal.torso end
    if k=='demon' and y>headBottom and y<chestY+6 and math.abs(x-cx)<half*.75 then rgb=pal.skin end
    if (k=='tree' or k=='golem') then rgb=pal.torso end
    if spec.pack=='greatsword' and math.abs(pc.rgbaR(q)-pc.rgbaB(q))<22 and pc.rgbaR(q)>90 then rgb={184,189,184} end
    p(shade(rgb,q))
  end end
  -- Garment seams and trim are clipped to actual moving cloth/armor surfaces.
  if k=='healer' or k=='emperor' or k=='soldier' then
    for y=math.floor(headBottom+4),math.floor(maxy-h*.15) do
      for dx=-1,1 do fittedDot(cx+dx,y,{194,158,79}) end
      if y>waist-2 and y<waist+1 then for x=math.floor(cx-half),math.floor(cx+half) do fittedDot(x,y,{139,113,65}) end end
    end
  elseif k=='barbarian' or k=='archer' or k=='inventor' then
    for x=math.floor(cx-half),math.floor(cx+half) do for dy=0,2 do fittedDot(x,waist+dy,{92,63,42}) end end
    fittedDot(cx,waist+1,{195,161,78})
  elseif k=='tree' then
    for y=math.floor(headBottom+6),math.floor(maxy-5) do for _,dx in ipairs({-6,5}) do fittedDot(cx+dx+math.sin(y*.22)*2,y,{91,77,50}) end end
  end
  -- Head accessories stay within a few pixels of the detected head, preserving the face.
  if k=='archer' or k=='healer' or k=='barbarian' or k=='brewer' or k=='demon' or k=='inventor' or k=='tree' then
    for y=math.floor(headTop),math.floor(headBottom) do for x=math.floor(cx-14),math.floor(cx+14) do
      if covered(x,y) then
        local q=original:getPixel(x,y)
        local hair=y<headY-2 or (k=='archer' and (math.abs(x-cx)>h*.065 or y<headTop+h*.08))
        if hair then gear:drawPixel(x,y,shade(k=='archer' and pal.cloth or pal.hair,q)) end
        if k=='brewer' and y>headY+3 then gear:drawPixel(x,y,shade(pal.hair,q)) end
        if front and y>headY-1 and y<headY+3 and math.abs(x-(cx+fx*3))<h*.055 then
          gear:drawPixel(x,y,shade(pal.skin,q,.13))
        end
      end
    end end
  end
  if front and k~='mech' then
    local eyeY=math.floor(headY+1);local eyeX=cx+fx*3
    fittedDot(eyeX-3,eyeY,k=='tree' and {224,194,103} or {43,37,39})
    fittedDot(eyeX+3,eyeY,k=='golem' and {229,209,122} or {43,37,39})
  end
  if k=='healer' or k=='demon' then
    local side=k=='healer' and 1 or -1
    line(gear,cx-7,headTop+6,cx-8,headTop-1,c(162,145,104),2)
    if side<0 then line(gear,cx+7,headTop+5,cx+9,headTop-2,c(162,112,165),2) end
  elseif k=='emperor' then
    poly(gear,{{cx-5,headTop+4},{cx-6,headTop-2},{cx,headTop+1},{cx+6,headTop-2},{cx+5,headTop+5}},c(204,166,82))
    local bx=cx+fx*5;line(gear,bx,headY+3,bx+fx*7,headY+5,c(180,135,56),3)
  elseif k=='inventor' then
    -- Hair grows from the helmet surface, never a floating crown.
    for n=-2,2 do oval(gear,cx+n*3,headTop+2+math.abs(n),5,4,c(202+n*5,161+n*4,63)) end
    if front then for sign=-1,1,2 do oval(gear,cx+sign*4,headY+2,4,3,c(78,67,44));oval(gear,cx+sign*4,headY+2,2,2,c(126,198,202)) end end
  elseif k=='tree' or k=='golem' then
    for _,dx in ipairs({-9,8}) do
      local ly=headBottom+6
      for y=math.floor(ly-3),math.floor(ly+3) do for x=math.floor(cx+dx-5),math.floor(cx+dx+5) do if covered(x,y) then gear:drawPixel(x,y,c(86+(x%3)*8,117+(y%3)*7,57)) end end end
    end
  end
  -- Find actual exposed hands in the current frame; weapons use these moving grip points.
  local function hand(sign)
    local best,xx,yy=-1,cx+sign*(half+6),chestY+5
    for y=math.max(0,math.floor(headBottom+3)),math.min(127,math.floor(waist+10)) do for x=0,127 do
      if covered(x,y) and (x-cx)*sign>half then
        local score=(x-cx)*sign-math.abs(y-(chestY+8))*.22
        if score>best then best=score;xx=x;yy=y end
      end
    end end
    return {xx-sign*2,yy}
  end
  local right,left=hand(fx>=0 and 1 or -1),hand(fx>=0 and -1 or 1)
  local hx,hy=right[1],right[2]
  local phase=frame/14;local release=.36
  local fire=motion=='attack' and math.sin(math.min(1,phase/release)*math.pi*.5) or 0
  local function grip(x,y)
    oval(gear,x,y,2,2,c(pal.skin[1],pal.skin[2],pal.skin[3]))
  end
  local function staff(spear)
    local tilt=motion=='attack' and fire*.65 or .1
    local tx,ty=fx*tilt,-1+math.abs(fy)*tilt
    line(gear,hx-tx*17,hy-ty*17,hx+tx*30,hy+ty*30,c(89,66,40),3)
    line(gear,hx-tx*17,hy-ty*17,hx+tx*30,hy+ty*30,c(177,145,76),1)
    local tipx,tipy=hx+tx*30,hy+ty*30
    if spear then poly(gear,{{tipx-3,tipy+4},{tipx+fx*4,tipy-7},{tipx+4,tipy+4},{tipx,tipy+8}},c(199,183,124))
    else
      line(gear,tipx-3,tipy-4,tipx-6,tipy+1,c(212,184,102),2)
      line(gear,tipx-6,tipy+1,tipx-2,tipy+6,c(212,184,102),2)
      line(gear,tipx-2,tipy+6,tipx+3,tipy+3,c(212,184,102),2)
    end
    grip(hx,hy)
  end
  if k=='healer' or k=='emperor' or k=='soldier' then staff(k~='healer')
  elseif k=='archer' then
    local bx,by=hx+fx*fire*5,hy+fy*fire*5
    -- The forearm is shaded and connected to the original wrist during bow extension.
    line(gear,hx,hy,bx,by,c(53,85,113),4);line(gear,hx,hy-1,bx,by-1,c(91,131,157),2)
    local arc=(fx>=0 and 1 or -1)*6
    line(gear,bx,by-17,bx+arc,by-8,c(115,187,207),2);line(gear,bx+arc,by-8,bx+arc,by+8,c(171,223,229),2);line(gear,bx+arc,by+8,bx,by+17,c(115,187,207),2)
    local pull=motion=='attack' and phase<release and fire*8 or 0
    line(gear,bx,by-17,bx-fx*pull,by,c(218,229,225),1);line(gear,bx-fx*pull,by,bx,by+17,c(218,229,225),1)
    if motion~='attack' or phase<release then
      line(gear,bx-fx*(10+pull),by-fy*(10+pull),bx+fx*14,by+fy*14,c(198,206,198),1)
      poly(gear,{{bx+fx*17,by+fy*17},{bx+fx*10-fy*3,by+fy*10+fx*3},{bx+fx*10+fy*3,by+fy*10-fx*3}},c(158,214,223))
    end
    grip(bx,by)
  elseif k=='demon' then
    for sign=-1,1,2 do
      local sx=cx+sign*half*.7;local sy=headBottom+9
      local reach=motion=='attack' and fire*14 or 0
      local points={{sx,sy},{sx+sign*12,sy+12},{sx+sign*(20+reach),sy+5},{sx+sign*(19+reach),sy-6}}
      for i=1,3 do line(back,points[i][1],points[i][2],points[i+1][1],points[i+1][2],c(91+i*12,45+i*5,112+i*9),3) end
    end
  elseif k=='brewer' then
    local rx,ry=9,12;local barrelx,barrely=hx+fx*4,hy+10
    oval(gear,barrelx,barrely,rx,ry,c(100,67,37));oval(gear,barrelx-2,barrely-1,rx-2,ry-2,c(143,103,61))
    for dx=-5,5,5 do line(gear,barrelx+dx,barrely-9,barrelx+dx,barrely+9,c(94,65,37),1) end
    for dy=-7,7,14 do line(gear,barrelx-7,barrely+dy,barrelx+7,barrely+dy,c(93,98,92),2) end
    grip(hx,hy)
  elseif k=='inventor' then
    line(gear,hx,hy,hx+fx*12,hy+fy*12,c(103,113,111),3)
    oval(gear,hx+fx*12,hy+fy*12,4,4,c(161,172,160));oval(gear,hx+fx*13,hy+fy*13-2,2,2,c(64,75,72));grip(hx,hy)
  elseif k=='mech' then
    line(gear,hx-3*fx,hy-3*fy,hx+fx*11,hy+fy*11,c(74,82,80),6)
    line(gear,hx-3*fx,hy-2-3*fy,hx+fx*10,hy-2+fy*10,c(159,132,79),2)
    local sx=cx-fx*12;line(back,sx,headBottom+9,sx-2,headTop+7,c(69,76,74),4);line(back,sx-1,headTop+7,sx+4,headTop+7,c(100,106,97),3)
    if front then for x=math.floor(cx-5),math.floor(cx+5) do fittedDot(x,headY+3,{93,170,182}) end end
  elseif k=='tree' then
    for sign=-1,1,2 do
      local hnd=sign>0 and right or left
      line(gear,hnd[1],hnd[2],hnd[1]+sign*6,hnd[2]-5,c(122,103,65),2)
      line(gear,hnd[1]+sign*6,hnd[2]-5,hnd[1]+sign*5,hnd[2]-11,c(122,103,65),2)
    end
  elseif k=='centaur' then
    local horse=draw.load(draw.root..'/Assets/DivineWar/Art/horse.png');local hw,hh=horse.width/4,horse.height/2
    local part=Image(horse,Rectangle((dir%4)*hw,math.floor(dir/4)*hh,hw,hh));local bb=part:shrinkBounds();part=Image(part,bb)
    part:resize(math.floor(bb.width*55/bb.height),55)
    for p in part:pixels() do local q=p();if pc.rgbaA(q)>100 then p(shade(pal.torso,q)) else p(0) end end
    for y=math.floor(waist),127 do for x=0,127 do body:drawPixel(x,y,0);gear:drawPixel(x,y,0) end end
    back:drawImage(part,Point(math.floor(cx-part.width/2),112-part.height))
  end
  return true
end
