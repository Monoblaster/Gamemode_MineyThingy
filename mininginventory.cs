function MiningInventory_BaseUse(%stack,%toolNum)
{
	Inventory::Display(%stack.client.player,%stack.client,true,0);
	%stack.push(Inventory(%stack.active.get(%toolNum).uiName));
	return;
}

datablock ItemData(MiningInventory_Inventory)
{
	uiname = "Inventory";
};

datablock ItemData(MiningInventory_Place)
{
	uiname = "Place";
};

datablock ItemData(MiningInventory_PlaceDirt)
{
	uiname = "Dirt";
};

datablock ItemData(MiningInventory_PlaceStone)
{
	uiname = "Rock";
};

datablock ItemData(MiningInventory_PlaceConcrete)
{
	uiname = "Concrete";
};

function MiningInventory_PlaceSelect(%stack,%toolNum)
{
	//TODO: Item images for placables would be cool

	%tool = %stack.active.get(%toolNum);
	return %stack.client.placableInventory[%tool.uiName] + 0 SPC %tool.uiName;
}

function MiningInventory_PlaceUse(%stack,%toolNum)
{
	if(!isObject(%stack.client.player))
	{
		return;
	}

	%tool = %stack.active.get(%toolNum);

	%start = %stack.client.player.getEyePoint();
	%brick = ContainerRayCast(%start, VectorAdd(VectorScale(%stack.client.player.getEyeVector(),6),%Start), $TypeMasks::FxBrickObjectType);
	if(%brick == 0 || %stack.client.placableInventory[%tool.uiName] <= 0)
	{
		return;
	}

	%pos = $mine.to(vectorSub(getWords(%brick,1,3),getWords(%brick,4,6)));
	%pos = vectorAdd(vectorAdd($mine.from(mFloor(getWord(%pos,0)) SPC mFloor(getWord(%pos,1)) SPC mFloor(getWord(%pos,2))),vectorScale(getWords(%brick,4,6),2)),"1 1 1");
	%type = Material(%tool.uiName);

	%angleId = getRandom(0,3);
	if (%angleId == 0)
	{
		%pos = %pos SPC " 1 0 0 0";
	}
	else if (%angleId == 1)
	{
		%pos = %pos SPC " 0 0 1" SPC $piOver2;
	}
	else if (%angleId == 2)
	{
		%pos = %pos SPC " 0 0 1" SPC $pi;
	}
	else if (%angleId == 3)
	{
		%pos = %pos SPC " 0 0 -1" SPC $piOver2;
	}

	%brick = new fxDTSBrick()
	{
		dataBlock = "brickMineCubeData";
		position = %pos;
		angleId = getRandom(0,3);
		isPlanted = true;
		isBasePlate = true;
		isMineable = true;
		printId = %type.printId;
		colorId = %type.colorId;
		material = %type;
	};
	%brick.setTransform(%pos);
	$mine.bricks.add(%brick);
	BrickGroup_888888.add(%brick);
	%plantError = %brick.plant();
	if(%plantError != 0 && %plantError != 2)
	{
		
		%brick.delete();
		return;
	}

	$mine.mineBrick[$mine.to(vectorSub(%pos,"1 1 1"))] = %brick;
	%brick.setTrusted(1);
	%stack.client.placableInventory[%tool.uiName]--;
	%stack.print();
}

datablock ItemData(MiningInventory_Build)
{
	uiname = "Build";
};



function MiningInventory_Init()
{
	%c = 3;
	%inv = Inventory("Base");
	%inv.cantClose = true;
	%inv.dontAutoOpen = true;
	%inv.use = "MiningInventory_BaseUse";
	%inv.set(%c++,"MiningInventory_Inventory");
	%inv.set(%c++,"MiningInventory_Place");
	%inv.set(%c++,"MiningInventory_Build");

	%c = -1;
	%inv = Inventory("Inventory");
	%inv.showNewItems = true;
	%inv.dontOverwrite = true;
	%inv.canUseTools = true;

	%c = -1;
	%inv = Inventory("Place");
	%inv.select = "MiningInventory_PlaceSelect";
	%inv.use = "MiningInventory_PlaceUse";
	%inv.set(%c++,"MiningInventory_PlaceDirt");
	%inv.set(%c++,"MiningInventory_PlaceStone");
	%inv.set(%c++,"MiningInventory_PlaceConcrete");

	%c = -1;
	%inv = Inventory("Build");
}
MiningInventory_Init();