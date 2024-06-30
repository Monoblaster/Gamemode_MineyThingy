exec("./inventoryutil.cs");
exec("./inventoryui.cs");
exec("./mininginventory.cs");
exec("./perlinNoise.cs");
exec("./octree.cs");
exec("./newPop.cs");
exec("./material.cs");
exec("./mine.cs");



datablock fxDTSBrickData (brick64CubeData)
{
	brickFile = "./bricks/64Cube.blb";
	category = "Baseplates";
	subCategory = "Mining";
	uiName = "64Cube";
};

datablock fxDTSBrickData(brickMineCubeData)
{
	brickFile = "./bricks/MineCube.blb";
	category = "Baseplates";
	subCategory = "Mining";
	uiName = "Mine Cube";
    hasPrint = 1;
	printAspectRatio = "ModTer";
};


//TODO: this is tempory mining
package Mining
{
	function fxDTSBrick::onActivate(%brick)
	{
		if(%brick.isMineable && %brick.material.name !$= "Rock")
		{
			%pos = $mine.To(%brick.getPosition());
			$mine.reveal(vectorAdd(%pos,"0 0 1"));
			$mine.reveal(vectorAdd(%pos,"0 0 -1"));
			$mine.reveal(vectorAdd(%pos,"1 0 0"));
			$mine.reveal(vectorAdd(%pos,"-1 0 0"));
			$mine.reveal(vectorAdd(%pos,"0 1 0"));
			$mine.reveal(vectorAdd(%pos,"0 -1 0"));
			if(isObject(%brick.material.getName() @ "Item"))
			{
				%item = new Item()
				{
					dataBlock = %brick.material.getName() @ "Item";
					position = %brick.getTransform();
				};
				%item.setVelocity(vectorNormalize(getRandom() - 0.5 SPC getRandom() - 0.5 SPC getRandom() - 0.5));
				%item.schedulePop();
			}
			else
			{
				//TODO: dirt probably add this to the player so they can build with it
			}
			
			%brick.delete();
			return;
		}
		
		parent::onActivate(%brick);
	}
};
activatePackage("Mining");

function Armor::onCollision (%this, %obj, %col, %vec, %speed)
{
	if (%obj.getState () $= "Dead")
	{
		return;
	}
	if (%col.getDamagePercent () >= 1)
	{
		return;
	}
	%colClassName = %col.getClassName ();
	if (%colClassName $= "Item")
	{
		%client = %obj.client;
		%colData = %col.getDataBlock ();
		%i = 0;
		// while (%i < %this.maxTools)
		// {
		// 	if (%obj.tool[%i] == %colData)
		// 	{
		// 		return;
		// 	}
		// 	%i += 1;
		// }
		%obj.pickup (%col);
	}
	else if (%colClassName $= "Player" || %colClassName $= "AIPlayer")
	{
		if (%col.getDataBlock ().canRide && %this.rideAble && %this.nummountpoints > 0)
		{
			if (getSimTime () - %col.lastMountTime <= $Game::MinMountTime)
			{
				return;
			}
			%colZpos = getWord (%col.getPosition (), 2);
			%objZpos = getWord (%obj.getPosition (), 2);
			if (%colZpos <= %objZpos + 0.2)
			{
				return;
			}
			%canUse = 0;
			if (isObject (%obj.spawnBrick))
			{
				%vehicleOwner = findClientByBL_ID (%obj.spawnBrick.getGroup ().bl_id);
			}
			if (isObject (%vehicleOwner))
			{
				if (getTrustLevel (%col, %obj) >= $TrustLevel::RideVehicle)
				{
					%canUse = 1;
				}
			}
			else 
			{
				%canUse = 1;
			}
			if (miniGameCanUse (%col, %obj) == 1)
			{
				%canUse = 1;
			}
			if (miniGameCanUse (%col, %obj) == 0)
			{
				%canUse = 0;
			}
			if (!%canUse)
			{
				if (!isObject (%obj.spawnBrick))
				{
					return;
				}
				%ownerName = %obj.spawnBrick.getGroup ().name;
				%msg = %ownerName @ " does not trust you enough to do that";
				if ($lastError == $LastError::Trust)
				{
					%msg = %ownerName @ " does not trust you enough to ride.";
				}
				else if ($lastError == $LastError::MiniGameDifferent)
				{
					if (isObject (%col.client.miniGame))
					{
						%msg = "This vehicle is not part of the mini-game.";
					}
					else 
					{
						%msg = "This vehicle is part of a mini-game.";
					}
				}
				else if ($lastError == $LastError::MiniGameNotYours)
				{
					%msg = "You do not own this vehicle.";
				}
				else if ($lastError == $LastError::NotInMiniGame)
				{
					%msg = "This vehicle is not part of the mini-game.";
				}
				commandToClient (%col.client, 'CenterPrint', %msg, 1);
				return;
			}
			for (%i = 0; %i < %this.nummountpoints; %i += 1)
			{
				if (%this.mountNode[%i] $= "")
				{
					%mountNode = %i;
				}
				else 
				{
					%mountNode = %this.mountNode[%i];
				}
				%blockingObj = %obj.getMountNodeObject (%mountNode);
				if (isObject (%blockingObj))
				{
					if (!%blockingObj.getDataBlock ().rideAble)
					{
						continue;
					}
					if (%blockingObj.getMountedObject (0))
					{
						continue;
					}
					%blockingObj.mountObject (%col, 0);
					if (%blockingObj.getControllingClient () == 0)
					{
						%col.setControlObject (%blockingObj);
					}
					%col.setTransform ("0 0 0 0 0 1 0");
					%col.setActionThread (root, 0);
					continue;
				}
				%obj.mountObject (%col, %mountNode);
				%col.setActionThread (root, 0);
				if (%i == 0)
				{
					if (%obj.isHoleBot)
					{
						if (%obj.controlOnMount)
						{
							%col.setControlObject (%obj);
						}
					}
					else if (%obj.getControllingClient () == 0)
					{
						%col.setControlObject (%obj);
					}
					if (isObject (%obj.spawnBrick))
					{
						%obj.lastControllingClient = %col;
					}
				}
				break;
			}
		}
	}
}

Material_Define("Dirt",8,2);
Material_Define("Rock",7,2);
Material_Define("Iron",4,2,true);

function test()
{
	$mine.delete();
	$mine = mine_create("0 0 0.2",2,2,Material("Dirt"));
	$mine.Lode(Material("Rock"),0,0.1,0.6);

	$mine.RandomBlobs(Material("Iron"),328,10,0.5);

	// Mine_RevealAll($mine);

	$mine.Air("6 6 6",2,2);
	$mine.RevealArea("5 5 5",4,4);
	ClientGroup.getObject(0).player.settransform($mine.From("7 7 6"));
}

//assets
//remap the mining cube so i have 6 unique sides
//make ore model and ore cube texture

//ideas:
// All materials have an item form
// when players start out they have access to all required tools and machines for mining.
// You can build tech buildings that when finish building unlock buildings related to their tech
// maybe you can get dirt and rocks? i dunno probably just decoration
// you can plce dirt and rock that you collect. dirt can be mined normally but rock must be blown up to collect
// unlike other materials you can just collect dirt and rock without it taking up space
// when a player dies they drop their materials and tools to make it easy to just steal other people's stuff
// win condition 1: destroy the other team's core
// win condition 2: collect the most of a very rare material on time up
// most things should probably be instant mine for the sake of brevity
// you can replace anything with a concrete block and then may build off of concrete blocks
// concrete blocks can be upgraded into a multitude of buildings
// when this happens it will become a ghost of the in progress building and if near a core will be built if it has the mats. if not players can supply it manually
// you can input materials into your base's core or a buildible mini core to store them in the "cloud"
// materials in the cloud will be used automatically if there is a mini core or core nearby. materials can also be taken out by players (except for objective mats)
// building ideas: item manufacturer (multiple tiers probably), turrets for base defense,  