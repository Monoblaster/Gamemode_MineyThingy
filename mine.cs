//TODO: cave ins
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

function Mine::OnAdd(%mine)
{
	$Mine::Set.add(%mine);
	%mine.bricks = new SimSet();
	%mine.noise = PerlinNoise_Create();
	%mine.octree = Octree_Create(%mine.width > %mine.height ? %mine.width * 16 : %mine.height * 16);
	%mine.blobCount = 0;
	%mine.lodeCount = 0;

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
		%newX = %x + 32 * (%i % %width);
		%newY = %y + 32 * (mFloor(%i / %width) % %width);

		if(%newX > %minX && %newX < %maxX && %newY > %minY && %newY < %maxY) //hollow cube
		{
			continue;
		}

		%brick = new fxDTSBrick()
		{
			dataBlock = "brick64CubeData";
			position = %newX SPC %newY SPC (%z + 32 * mFloor(%i / %cubesPerFloor));
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
	return vectorScale(vectorSub(%pos,%mine.point),1/2);
}

function Mine::From(%mine,%pos)
{
	return vectorAdd(vectorScale(%pos,2),%mine.point);
}

//plants a brick representing a place within the mine's grid starting at the mine's point and ending later idk lol
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


	if(%blobs $= "") // no blobs so we noisey
	{

		%noise = %mine.noise;
		%count = %mine.lodeCount;
		for(%i = 0; %i < %count; %i++)
		{
			%currLode = %mine.lodeInfo[%i];
			%offset = getWord(%currLode,1) + 0.1; 
			%newpos = vectorScale(vectorAdd(%pos,%offset SPC %offset SPC %offset),getWord(%currLode,2));
			if(%noise.Sample(getWord(%newpos,0),getWord(%newpos,1),getWord(%newpos,2)) >= getWord(%currLode,3))
			{
				%type = getWord(%currLode,0);
				break;
			}
		}

	}
	else //see what we get from our in range blobs
	{
		%count = getWordCount(%blobs);//would be nice if we could just remove a blob from the octree after it has run out of things but alas we cannot right now
		for(%i = 0; %i < %count; %i++)
		{
			%currBlob = getWord(%blobs,%i);
			if(vectorDist(%pos,%mine.BlobPosition[%currBlob]) > getWord(%mine.BlobInfo[%currBlob],1))
			{
				continue;
			}

			if(!%mine.BlobVolumeRemaining[%currBlob] || getRandom() > %mine.BlobRemaining[%currBlob] / %mine.BlobVolumeRemaining[%currBlob])
			{
				%mine.BlobVolumeRemaining[%currBlob]--;
				continue;
			}

			%type = firstWord(%mine.BlobInfo[%currBlob]);
			%mine.BlobRemaining[%currBlob]--;
			%mine.BlobVolumeRemaining[%currBlob]--;
			break;
		}

		for(%i = %i + 1; %i < %count; %i++) //removing the volume from the remaining blobs
		{
			%currBlob = getWord(%blobs,%i);
			if(vectorDist(%pos,%mine.BlobPosition[%currBlob]) > getWord(%mine.BlobInfo[%currBlob],1))
			{
				continue;
			}

			%mine.BlobVolumeRemaining[%currBlob]--;
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
		talk("FAILED TO REVEAL BRICK FUCK!!!!!!!");
		%brick.delete();
		return;
	}
	%mine.mineBrick[%pos] = %brick;
	%brick.setTrusted(1);
}

//TODO: make a cache of these values so i can reuse them later for faster execution
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
	%xC = getWord(%center,0);
	%yC = getWord(%center,1);
	%zC = getWord(%center,2);
	%diameter = %radius * 2 + 1;

	%count = %diameter * %diameter * %diameter;
	for(%i = 0; %i < %count; %i++)
	{
		%newX = (%i % %diameter) + %x;
		%newY = (mFloor(%i / %diameter) % %diameter) + %y;
		%newZ =  mFloor(%i / (%diameter * %diameter)) + %z;
		if(%mine.mineBrick[%newX SPC %newY SPC %newZ] !$= "" || (%xC - %newX) * (%xC - %newX) + (%yC - %newY) * (%yC - %newY) + (%zC - %newZ) * (%zC - %newZ) > %radius * %radius)
		{
			continue;
		}
		%mine.reveal(%newX SPC %newY SPC %newZ);
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

