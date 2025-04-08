//mines are boxes where sides are barriers that cannot be mined through
//when a box is mined the boxes around it are revealed
//the closer the player mines up to 100% before the top the more likely that column collapses
//when a column collapses it falls in if it is connected to the top barrier from the top barrier to the bottom new bricks will be created
if(!isObject($Mine::Set))
{
	$Mine::Set = new SimSet();
}

//bottom is one of the corners idk it doesn't really matter
//size is multiplied by 64 so it's always a multiple of 64
//this is to make it easie to make the barriers
function Mine_Create(%bottomCorner,%width,%height,%material)
{
	return new ScriptObject()
	{
		point = %bottomCorner;
		width = %width;
		height = %height;
		material = %material;
		class = "Mine";
	};
}

//gives us a script object we can add positions to
//we can then add this list of positions to one of the queues
function Mine::Queue(%mine,%callback)
{
	return new ScriptObject("MineQueue")
	{
		index = 0;
		count = 0;
		callback = %callback;
		mine = %mine;
	};
}

function MineQueue::Reveal(%queue,%air)
{
	%queue.air = %air;
	%queue.mine.revealQueue.add(%queue);
}

//reveals and deletes positions in the order they were added
//created bricks are made as dust which will be revealed later
//this function is a little spaghetti but that's ok :)
function Mine_RevealQueue(%mine,%lastStartTime)
{
	if(%mine.revealQueue.getCount() == 0) // sleep until something happens
	{
		Schedule(33,%mine,"Mine_RevealQueue",%mine,getSimTime());
		return;
	}

	%thisStartTime = getSimTime();
	%delta = %thisStartTime - %lastStartTime - 33;
	
	%queue = %mine.revealQueue.getObject(0);

	%type = "DustMaterial";

	if(%queue.air)
	{
		%noise = %mine.noise;
		%offset = getWord(%mine.lodeInfo[0],1) + 0.1; 
		%scale = getWord(%mine.lodeInfo[0],2);
		%threshold = getWord(%mine.lodeInfo[0],3);
	}

	%count = getMin(%queue.count,%queue.index + (1 - %delta / 25) * 100);
	for(%i = %queue.index; %i < %count; %i++)
	{
		%pos = %queue.pos[%i];
		if(%queue.air)
		{
			%brick = %mine.mineBrick[%pos];
			if(%brick.material !$= "RockMaterial" && %noise.Sample((getWord(%pos,0) + %offset) * %scale, (getWord(%pos,1) + %offset) * %scale, (getWord(%pos,2) + %offset) * %scale) < %threshold)
			{
				if(isObject(%brick))
				{
					%brick.delete();
				}
				%mine.mineBrick[%pos] = true;
				continue;
			}

			if(%brick == true)
			{
				continue;
			}

			%type = "GravelMaterial";
			if(isObject(%brick))
			{
				if(%brick.material $= %type)
				{
					%mine.mineBrick[%pos] = true;
					%brick.delete();
					continue;
				}
				%brick.delete();
			}
		}

		%angleId = getRandom(0,3);
		if (%angleId == 0)
		{
			%transform = vectorAdd(vectorScale(vectorAdd(%queue.pos[%i],"0.5 0.5 0.5"),2),%mine.point) SPC " 1 0 0 0";
		}
		else if (%angleId == 1)
		{
			%transform = vectorAdd(vectorScale(vectorAdd(%queue.pos[%i],"0.5 0.5 0.5"),2),%mine.point) SPC " 0 0 1" SPC $piOver2;
		}
		else if (%angleId == 2)
		{
			%transform = vectorAdd(vectorScale(vectorAdd(%queue.pos[%i],"0.5 0.5 0.5"),2),%mine.point) SPC " 0 0 1" SPC $pi;
		}
		else if (%angleId == 3)
		{
			%transform = vectorAdd(vectorScale(vectorAdd(%queue.pos[%i],"0.5 0.5 0.5"),2),%mine.point) SPC " 0 0 -1" SPC $piOver2;
		}

		//plant as a dust brick
		%brick = new fxDTSBrick()
		{
			dataBlock = "brickMineCubeData";
			position = %transform;
			angleId = %angleId;
			isPlanted = true;
			isBasePlate = true;
			isMineable = true;
			printId = %type.printId;
			colorId = %type.colorId;
			material = %type;
		};
		%brick.setTransform(%transform);
		%mine.bricks.add(%brick);
		BrickGroup_888888.add(%brick);
		%plantError = %brick.plant();
		if(%plantError != 0 && %plantError != 2)
		{
			%brick.delete();
			continue;
		}
		%mine.mineBrick[%pos] = %brick;
		%brick.setTrusted(1);
	}

	%queue.index = %i;

	if(%i >= %queue.count)
	{
		%queue.index = 0;
		call(%queue.callback,%queue);
		%mine.revealQueue.pushToBack(%queue);
		%mine.revealQueue.remove(%queue);
		if(!%queue.air)
		{
			%mine.dustQueue.add(%queue);
		}
		else if(!isFunction(%queue.callback))
		{
			%queue.delete();
		}
	}

	Schedule(33,%mine,"Mine_RevealQueue",%mine,%thisStartTime);
}

function Mine_DustQueue(%mine,%lastStartTime)
{
	if(%mine.dustQueue.getCount() == 0) // sleep until something happens
	{
		Schedule(33,%mine,"Mine_DustQueue",%mine,getSimTime());
		return;
	}

	%thisStartTime = getSimTime();
	%delta = %thisStartTime - %lastStartTime - 33;
	
	%queue = %mine.dustQueue.getObject(0);
	%count = getMin(%queue.count,%queue.index + (1 - %delta / 25) * 100);
	for(%i = %queue.index; %i < %count; %i++)
	{
		%pos = %queue.pos[%i];
		if(!isObject(%mine.mineBrick[%queue.pos[%i]]))
		{
			continue;
		}
		%x = getWord(%pos,0);
		%y = getWord(%pos,1);
		%z = getWord(%pos,2);

		%type = %mine.material;

		%blobs = %mine.octree.searchPoint(%x,%y,%z);

		%partOfBlob = false;
		if(%blobs !$= "")
		{
			%blobcount = getWordCount(%blobs);//would be nice if we could just remove a blob from the octree after it has run out of things but alas we cannot right now
			for(%j = 0; %j < %blobcount; %j++)
			{
				%currBlob = getWord(%blobs,%j);
				if(vectorDist(%pos,%mine.BlobPosition[%currBlob]) > getWord(%mine.BlobInfo[%currBlob],1))
				{
					continue;
				}

				if(!%mine.BlobVolumeRemaining[%currBlob] || getRandom() > %mine.BlobRemaining[%currBlob] / %mine.BlobVolumeRemaining[%currBlob])
				{
					%mine.BlobVolumeRemaining[%currBlob]--;
					continue;
				}

				%partOfBlob = true;
				%type = firstWord(%mine.BlobInfo[%currBlob]);
				%mine.BlobRemaining[%currBlob]--;
				%mine.BlobVolumeRemaining[%currBlob]--;
				break;
			}

			for(%j = %j + 1; %j < %blobcount; %j++) //removing the volume from the remaining blobs
			{
				%currBlob = getWord(%blobs,%j);
				if(vectorDist(%pos,%mine.BlobPosition[%currBlob]) > getWord(%mine.BlobInfo[%currBlob],1))
				{
					continue;
				}

				%mine.BlobVolumeRemaining[%currBlob]--;
			}
		}

		if(!%partOfBlob) // no blobs so we noisey
		{
			%noise = %mine.noise;
			%lodecount = %mine.lodeCount;
			for(%j = 0; %j < %lodecount; %j++)
			{
				%currLode = %mine.lodeInfo[%j];
				%offset = getWord(%currLode,1) + 0.1; 
				%scale = getWord(%currLode,2);
				if(%noise.Sample((%x + %offest) * %scale,(%y + %offest) * %scale,(%z + %offest) * %scale) >= getWord(%currLode,3))
				{
					%type = getWord(%currLode,0);
					break;
				}
			}
		}

		if(%type $= "RockMaterial")
		{
			%type = "GravelMaterial";
		}

		%mine.mineBrick[%pos].setPrint(%type.printId);
		%mine.mineBrick[%pos].setColor(%type.colorId);
		%mine.mineBrick[%pos].material = %type;
	}

	%queue.index = %i;

	if(%i >= %queue.count)
	{
		%mine.dustQueue.remove(%queue);
		%queue.delete();
	}

	Schedule(33,%mine,"Mine_DustQueue",%mine,%thisStartTime);
}

function Mine::OnAdd(%mine)
{
	$Mine::Set.add(%mine);
	%mine.bricks = new SimSet();
	%mine.noise = PerlinNoise_Create();
	%mine.octree = Octree_Create(%mine.width > %mine.height ? %mine.width * 16 : %mine.height * 16);
	%mine.blobCount = 0;
	%mine.lodeCount = 0;

	%mine.revealQueue = new SimSet();
	Schedule(33,%mine,"Mine_RevealQueue",%mine,getSimTime());

	%mine.dustQueue = new SimSet();
	Schedule(33,%mine,"Mine_DustQueue",%mine,getSimTime());

	//make barriers
	%x = getWord(%mine.point,0) + 16;
	%y = getWord(%mine.point,1) + 16;
	%z = getWord(%mine.point,2) - 0.2;
	%width = %mine.width;
	%height = %mine.height;
	%group = BrickGroup_888888;
	
	//making floor
	%numBaseplates = mPow(%mine.width,2);
	for(%i = 0; %i < %numBaseplates; %i++)
	{
		%brick = new fxDTSBrick()
		{
			dataBlock = "brick64x64fData";
			position = (%x + 32 * (%i % %width)) SPC (%y + 32 * mFloor(%i / %width)) SPC %z;
			isPlanted = true;
			isBasePlate = true;
			isBarrier = true;
			colorid = 16;
		};
		%mine.bricks.add(%brick);
		%group.add(%brick);
		%plantError = %brick.plant();
		if(%plantError != 0 && %plantError != 2)
		{
			talk("FAILED TO CREATE MINE!!!");
			%mine.delete();
			return;
		}
		%brick.setTrusted(1);
	}

	//making ceiling
	%z = getWord(%mine.point,2) + %height * 32;
	for(%i = 0; %i < %numBaseplates; %i++)
	{
		%brick = new fxDTSBrick()
		{
			dataBlock = "brick64x64fData";
			position = (%x + 32 * (%i % %width)) SPC (%y + 32 * mFloor(%i / %width)) SPC %z;
			isPlanted = true;
			isBasePlate = true;
			isBarrier = true;
			colorid = 16;
		};
		%mine.bricks.add(%brick);
		%group.add(%brick);
		%plantError = %brick.plant();
		if(%plantError != 0 && %plantError != 2)
		{
			talk("FAILED TO CREATE MINE!!!");
			%mine.delete();
			return;
		}
		%brick.setTrusted(1);
	}

	%x = getWord(%mine.point,0) - 16;
	%y = getWord(%mine.point,1) - 16;
	%z = getWord(%mine.point,2) + 16;

	%minX = getWord(%mine.point,0);
	%maxX = getWord(%mine.point,0) + 32 * %width;

	%minY = getWord(%mine.point,1);
	%maxY = getWord(%mine.point,1) + 32 * %width;
	//making walls
	%width += 2;
	%cubesPerFloor = mPow(%width,2);
	%numCubes = %cubesPerFloor * %height;
	for(%i = 0; %i < %numCubes; %i++)
	{
		%nx = %x + 32 * (%i % %width);
		%ny = %y + 32 * (mFloor(%i / %width) % %width);

		if(%nx > %minX && %nx < %maxX && %ny > %minY && %ny < %maxY) //hollow cube
		{
			continue;
		}

		%brick = new fxDTSBrick()
		{
			dataBlock = "brick64CubeData";
			position = %nx SPC %ny SPC (%z + 32 * mFloor(%i / %cubesPerFloor));
			isPlanted = true;
			isBasePlate = true;
			isBarrier = true;
			colorid = 16;
		};
		%mine.bricks.add(%brick);
		%group.add(%brick);
		%plantError = %brick.plant();
		if(%plantError != 0 && %plantError != 2)
		{
			talk("FAILED TO CREATE MINE!!!");
			%mine.delete();
			return;
		}
		%brick.setTrusted(1);
	}
}

function Mine::OnRemove(%mine)
{
	%mine.noise.delete();
	%mine.octree.delete();

	//remove all bricks
	%mine.bricks.deleteAll(); // this will probably cause lag lol
	%mine.bricks.delete();
}

//adds a mterial blob to the mine
function Mine::Blob(%mine,%material,%position,%volume,%percent)
{
	%width = %mine.width * 16 - 1;
	%height = %mine.height * 16 - 1;
	%radius = mCeil(mPow((3 * %volume) / (4 * $Pi),1/3));
	%mine.octree.insert(%mine.blobCount,
		vectorSub(%position,getMin(%radius,getWord(%position,0)) SPC getMin(%radius,getWord(%position,1)) SPC getMin(%radius,getWord(%position,2))),
		getMin(getMin(getMin(%radius * 2,%width - getWord(%position,0),%width - getWord(%position,1))),%height - getWord(%position,2)));

	%mine.positionBlob[%position] = %mine.blobCount;
	%mine.BlobPosition[%mine.blobCount] = %position;
	%mine.BlobVolumeRemaining[%mine.blobCount] = %volume;
	%mine.BlobRemaining[%mine.blobCount] = round(%volume * %percent);
	%mine.blobInfo[%mine.blobCount++ - 1] = %material SPC %radius;
}

function Mine::RandomBlobs(%mine,%material,%count,%volume,%percent)
{
	%width = %mine.width * 16;
	%height = %mine.height * 16;
	%count = getMin(%count + %mine.blobCount, %width * %width * %height);

	%radius = mCeil(mPow((3 * %volume) / (4 * $Pi),1/3));

	%width--;
	%height--;

	%c = 0;
	while(%c < %count)
	{
		%position = getRandom(0,%width) SPC getRandom(0,%width) SPC getRandom(0,%height);
		if(%mine.positionBlob[%position] !$= "")
		{
			continue;
		}

		%mine.octree.insert(%mine.blobCount,
			vectorSub(%position,getMin(%radius,getWord(%position,0)) SPC getMin(%radius,getWord(%position,1)) SPC getMin(%radius,getWord(%position,2))),
			getMin(getMin(getMin(%radius * 2,%width - getWord(%position,0)),%width - getWord(%position,1)),%height - getWord(%position,2)));

		%mine.positionBlob[%position] = %mine.blobCount;
		%mine.BlobPosition[%mine.blobCount] = %position;
		%mine.BlobVolumeRemaining[%mine.blobCount] = %volume;
		%mine.BlobRemaining[%mine.blobCount] = round(%volume * %percent);
		%mine.blobInfo[%mine.blobCount++ - 1] = %material SPC %radius;

		%c++;
	}
}

//adds a material lode to the mine
function Mine::Lode(%mine,%material,%offset,%scale,%threshold)
{
	%mine.lodeInfo[%mine.lodeCount++ - 1] = %material SPC %offset SPC %scale SPC %threshold;
}

function Mine::To(%mine,%pos)
{
	%a = vectorScale(vectorSub(%pos,%mine.point),1/2);
	return mFloor(getWord(%a,0)) SPC mFloor(getWord(%a,1)) SPC mFloor(getWord(%a,2));
}

function Mine::From(%mine,%pos)
{
	return vectorAdd(vectorScale(%pos,2),%mine.point);
}

// plants a brick representing a place within the mine's grid starting at the mine's point and ending later idk lol
function Mine::Reveal(%mine,%pos)
{
	%x = mFloor(getWord(%pos,0));
	%y = mFloor(getWord(%pos,1));
	%z = mFloor(getWord(%pos,2));

	%pos = %x SPC %y SPC %z;
	if(%mine.mineBrick[%pos] !$= "") //air or already revealed
	{
		return;
	}

	if(%x < 0 || %x >= %mine.width * 16 || %y < 0 || %y >= %mine.width * 16 || %z < 0 || %z >= %mine.height * 16)
	{
		return;
	}
	%type = %mine.material;


	%blobs = $mine.octree.searchPoint(%x,%y,%z);


	%partOfBlob = false;
	if(%blobs !$= "")
	{
		%blobcount = getWordCount(%blobs);//would be nice if we could just remove a blob from the octree after it has run out of things but alas we cannot right now
		for(%j = 0; %j < %blobcount; %j++)
		{
			%currBlob = getWord(%blobs,%j);
			if(vectorDist(%pos,%mine.BlobPosition[%currBlob]) > getWord(%mine.BlobInfo[%currBlob],1))
			{
				continue;
			}

			if(!%mine.BlobVolumeRemaining[%currBlob] || getRandom() > %mine.BlobRemaining[%currBlob] / %mine.BlobVolumeRemaining[%currBlob])
			{
				%mine.BlobVolumeRemaining[%currBlob]--;
				continue;
			}

			%partOfBlob = true;
			%type = firstWord(%mine.BlobInfo[%currBlob]);
			%mine.BlobRemaining[%currBlob]--;
			%mine.BlobVolumeRemaining[%currBlob]--;
			break;
		}

		for(%j = %j + 1; %j < %blobcount; %j++) //removing the volume from the remaining blobs
		{
			%currBlob = getWord(%blobs,%j);
			if(vectorDist(%pos,%mine.BlobPosition[%currBlob]) > getWord(%mine.BlobInfo[%currBlob],1))
			{
				continue;
			}

			%mine.BlobVolumeRemaining[%currBlob]--;
		}
	}

	if(!%partOfBlob) // no blobs so we noisey
	{
		%noise = %mine.noise;
		%lodecount = %mine.lodeCount;
		for(%j = 0; %j < %lodecount; %j++)
		{
			%currLode = %mine.lodeInfo[%j];
			%offset = getWord(%currLode,1) + 0.1; 
			%scale = getWord(%currLode,2);
			if(%noise.Sample((%x + %offest) * %scale,(%y + %offest) * %scale,(%z + %offest) * %scale) >= getWord(%currLode,3))
			{
				%type = getWord(%currLode,0);
				break;
			}
		}
	}

	if(%type $= "")
	{
		return;
	}

	%angleId = getRandom(0,3);
	%transform = vectorAdd(vectorAdd(%mine.point,"1 1 1"),vectorScale(%x SPC %y SPC %z,2));
	if (%angleId == 0)
	{
		%transform = %transform SPC " 1 0 0 0";
	}
	else if (%angleId == 1)
	{
		%transform = %transform SPC " 0 0 1" SPC $piOver2;
	}
	else if (%angleId == 2)
	{
		%transform = %transform SPC " 0 0 1" SPC $pi;
	}
	else if (%angleId == 3)
	{
		%transform = %transform SPC " 0 0 -1" SPC $piOver2;
	}


	%brick = new fxDTSBrick()
	{
		dataBlock = "brickMineCubeData";
		position = %transform;
		angleId = getRandom(0,3);
		isPlanted = true;
		isBasePlate = true;
		isMineable = true;
		printId = %type.printId;
		colorId = %type.colorId;
		material = %type;
	};
	%brick.setTransform(%transform);
	%mine.bricks.add(%brick);
	BrickGroup_888888.add(%brick);
	%plantError = %brick.plant();
	if(%plantError != 0 && %plantError != 2)
	{
		%brick.delete();
		return false;
	}
	%mine.mineBrick[%pos] = %brick;
	%brick.setTrusted(1);
	return true;
}

//function to be used for spherical explosions
function Mine::Explode(%mine,%point,%radius)
{
	%shellRadius = %radius + 1;
	%point = mFloor(getWord(%point,0)) SPC mFloor(getWord(%point,1)) SPC mFloor(getWord(%point,2));
	%x = getWord(%point,0);
	%y = getWord(%point,1);
	%z = getWord(%point,2);
	%center = vectorAdd(%point,%shellRadius SPC %shellRadius SPC %shellRadius);
	%cx = getWord(%center,0);
	%cy = getWord(%center,1);
	%cz = getWord(%center,2);
	%outerRadiusSquare = %shellRadius * %shellRadius;
	%innerRadiusSquare = %radius * %radius;
	%diameter = %shellRadius * 2;
	
	//remove some volume from in range blobs
	%blobs = %mine.octree.searchArea(%x,%y,%z,%radius * 2);

	%blobcount = getWordCount(%blobs); //blow some ore up
	for(%j = 0; %j < %blobcount; %j++)
	{
		%currBlob = getWord(%blobs,%j);
		%threshold = getWord(%mine.BlobInfo[%currBlob],1) + %radius + 1;
		if(vectorDist(%point,%mine.BlobPosition[%currBlob]) > %threshold)
		{
			continue;
		}
		%mine.BlobVolumeRemaining[%currBlob] -=  (1 - vectorDist(%point,%mine.BlobPosition[%currBlob]) / %threshold) * %mine.BlobVolumeRemaining[%currBlob];
	}

	%diameter += 1;

	%queue = %mine.queue();
	%deleteQueue = %mine.queue();

	%count = %diameter * %diameter * %diameter;
	for(%i = 0; %i < %count; %i++)
	{
		%nx = (%i % %diameter) + %x;
		%ny = (mFloor(%i / %diameter) % %diameter) + %y;
		%nz =  mFloor(%i / (%diameter * %diameter)) + %z;
		
		if((%cx - %nx) * (%cx - %nx) + (%cy - %ny) * (%cy - %ny) + (%cz - %nz) * (%cz - %nz) > %outerRadiusSquare)
		{
			continue;
		}
		
		if((%cx - %nx) * (%cx - %nx) + (%cy - %ny) * (%cy - %ny) + (%cz - %nz) * (%cz - %nz) <= %innerRadiusSquare)
		{
			%deleteQueue.pos[%deleteQueue.count++ - 1] = %nx SPC %ny SPC %nz;
			continue;
		}

		if(%mine.mineBrick[%nx SPC %ny SPC %nz] !$= "")
		{
			continue;
		}

		%queue.pos[%queue.count++ - 1] = %nx SPC %ny SPC %nz;
	}
	%queue.reveal();
	%deleteQueue.reveal(true);
}

function Mine::AirCube(%mine,%point,%width,%height)
{
	%point = mFloor(getWord(%point,0)) SPC mFloor(getWord(%point,1)) SPC mFloor(getWord(%point,2));

	%count = %width * %width * %height;
	for(%i = 0; %i < %count; %i++)
	{
		%pos = vectorAdd((%i % %width) SPC  (mFloor(%i / %width) % %width) SPC mFloor(%i / (%width * %width)),%point);
		if(isObject(%mine.mineBrick[%pos]))
		{
			%mine.mineBrick[%pos].delete();
			continue;
		}
		%mine.mineBrick[%pos] = true;
	}
}

function Mine::RevealCube(%mine,%point,%width,%height)
{
	%point = mFloor(getWord(%point,0)) SPC mFloor(getWord(%point,1)) SPC mFloor(getWord(%point,2));

	%count = %width * %width * %height;
	for(%i = 0; %i < %count; %i++)
	{
		
		%mine.reveal(vectorAdd((%i % %width) SPC (mFloor(%i / %width) % %width) SPC mFloor(%i / (%width * %width)), %point));
	}
}

function Mine::AirSphere(%mine,%point,%radius)
{
	%point = mFloor(getWord(%point,0)) SPC mFloor(getWord(%point,1)) SPC mFloor(getWord(%point,2));
	%center = vectorAdd(%point,%radius SPC %radius SPC %radius);
	%diameter = %radius * 2 + 1;

	%count = %diameter * %diameter * %diameter;
	for(%i = 0; %i < %count; %i++)
	{
		%pos = vectorAdd((%i % %diameter) SPC  (mFloor(%i / %diameter) % %diameter) SPC mFloor(%i / (%diameter * %diameter)),%point);
		if(vectorDist(%pos,%center) > %radius)
		{
			continue;
		}

		if(isObject(%mine.mineBrick[%pos]))
		{
			%mine.mineBrick[%pos].delete();
			continue;
		}

		%mine.mineBrick[%pos] = true;
	}
}

function Mine::RevealSphere(%mine,%point,%radius)
{
	%point = mFloor(getWord(%point,0)) SPC mFloor(getWord(%point,1)) SPC mFloor(getWord(%point,2));
	%x = getWord(%point,0);
	%y = getWord(%point,1);
	%z = getWord(%point,2);
	%center = vectorAdd(%point,%radius SPC %radius SPC %radius);
	%cx = getWord(%center,0);
	%cy = getWord(%center,1);
	%cz = getWord(%center,2);
	%radiusSquare = %radius * %radius;
	%diameter = %radius * 2 + 1;

	%count = %diameter * %diameter * %diameter;
	for(%i = 0; %i < %count; %i++)
	{
		%nx = (%i % %diameter) + %x;
		%ny = (mFloor(%i / %diameter) % %diameter) + %y;
		%nz =  mFloor(%i / (%diameter * %diameter)) + %z;
		if(%mine.mineBrick[%nx SPC %ny SPC %nz] !$= "" || (%cx - %nx) * (%cx - %nx) + (%cy - %ny) * (%cy - %ny) + (%cz - %nz) * (%cz - %nz) > %radiusSquare)
		{
			continue;
		}
		%mine.reveal(%nx SPC %ny SPC %nz);
	}
}

// this is for debugging only lol
function Mine_RevealAll(%mine)
{
	%width = %mine.width * 16;
	%count = %width * %width * %mine.height * 16;
	for(%i = 0; %i < %count; %i++)
	{
		%pos = (%i % %width) SPC (mFloor(%i / %width) % %width) SPC  mFloor(%i / (%width * %width));
		%mine.reveal(%pos);
	}
}