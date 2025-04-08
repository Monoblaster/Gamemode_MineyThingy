exec("./inventoryutil.cs");
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

datablock ParticleData(MiningBombStaticParticle1)
{
	textureName = "./particles/spark_01.png";

	inheritedVelFactor = 1;
	lifetimeMS = 100;
	lifetimeVarianceMS = 50;

	sizes[0] = 1;
	colors[0] = "0 1 1 1";
	times[0] = 0;

	sizes[1] = 1;
	colors[1] = "0 1 1 0";
	times[1] = 1;
};

datablock ParticleData(MiningBombStaticParticle2 : MiningBombStaticParticle1)
{
	textureName = "./particles/spark_02.png";
};

datablock ParticleData(MiningBombStaticParticle3 : MiningBombStaticParticle1)
{
	textureName = "./particles/spark_03.png";
};

datablock ParticleData(MiningBombStaticParticle4 : MiningBombStaticParticle1)
{
	textureName = "./particles/spark_04.png";
};

datablock particleEmitterData(MiningBombStaticEmitter)
{
	particles = "MiningBombStaticParticle1" TAB "MiningBombStaticParticle2" TAB "MiningBombStaticParticle3" TAB "MiningBombStaticParticle4";
	lifetimeMS = 2000;
	lifetimeVarianceMS = 0;
	ejectionPeriodMS = 100;
	orientParticles = true;
};

datablock DebrisData(MiningBombDebris)
{
	emitters[0] = "MiningBombStaticEmitter";
	shapeFile = "./shapes/bombdepleted.dts";
	lifetime = 5.0;
	minSpinSpeed = -1000;
	maxSpinSpeed = 1000;
	elasticity = 0.8;
	friction = 0.2;
	numBounces = 2;
	staticOnMaxBounce = true;
	snapOnMaxBounce = false;
	fade = true;

	gravModifier = 1;
};

datablock AudioDescription (MiningBombExplosionSoundDescription)
{
	volume = 1;
	isLooping = 0;
	is3D = 1;
	ReferenceDistance = 10;
	maxDistance = 100;
	type = $SimAudioType;
};

datablock AudioProfile (MiningBombExplosionSound)
{
	fileName = "./sounds/bombexplosion.wav";
	description = MiningBombExplosionSoundDescription;
	preload = 1;
};

datablock ExplosionData(MiningBombExplosion)
{
	lifetimeMs = 100;
	soundProfile = "MiningBombExplosionSound";

	lightStartColor = "0 1 1";
	lightStartRadius = 10;

	lightEndColor = "0 1 1";
	lightEndRadius = 0;

	debris = "MiningBombDebris";
	debrisNum = 1;
	debrisNumVariance = 0;
	debrisPhiMin = 0;
	debrisPhiMax = 360;
	debrisThetaMin = 350;
	debrisThetaMax = 360;
	debrisVelocity = 6;
	debrisVelocityVariance = 0;

	particleEmitter = MiningBombStaticEmitter;
	particleRadius = 10;
	particleDensity = 100;
};

datablock ProjectileData(MiningBombProjectile)
{
	projectileShapeName = "./shapes/bombprojectile.dts";

	muzzleVelocity      = 50;
	velInheritFactor    = 1;

	armingDelay         = 20000;
	lifetime            = 20000;
	fadeDelay           = 20000;
	bounceElasticity    = 0.1;
	bounceFriction      = 0;
	isBallistic         = true;
	gravityMod = 0.50;

	hasLight = true;
	lightColor = "0 1 1";
	lightRadius = 10;

	explosion = "MiningBombExplosion";

	miningRadius = 3;

	uiName = "Mining Bomb";
};

datablock ProjectileData(MiningBombNoLightProjectile : MiningBombProjectile)
{
	className = "MiningBombProjectile";
	hasLight = false;
};

datablock AudioProfile (CountDownBeepSound)
{
	fileName = "./sounds/countdownbeep.wav";
	description = AudioClosest3d;
	preload = 1;
};

datablock AudioProfile (ArmBeepSound)
{
	fileName = "./sounds/armbeep.wav";
	description = AudioClosest3d;
	preload = 1;
};

function MiningBombProjectile::ArmLoop(%data,%proj)
{
	if(%proj.oldPos == %proj.getPosition())
	{
		%proj.landTime = getSimTime();
		%data.BeepLoop(%proj);
		return;
	}
	%proj.oldPos = %proj.getPosition();
	%data.schedule(100,"ArmLoop",%proj);
}

function MiningBombProjectile::BeepLoop(%data,%proj)
{
	%elapsedMs = getSimTime() - %proj.landTime;

	if(%proj.getDataBlock().getName() $= "MiningBombProjectile")
	{
		%proj.setDataBlock("MiningBombNoLightProjectile");
	}
	else
	{
		%proj.setDataBlock("MiningBombProjectile");
	}

	if(%elapsedMS > 2500)
	{
		%proj.explode();
		return;
	}
	ServerPlay3D("CountDownBeepSound", %proj.getPosition());
	%data.schedule(getMax((1 - %elapsedMS / 2500) * 1000,100),"BeepLoop",%proj);
}

function MiningBombProjectile::OnExplode(%data,%obj,%pos)
{
	//TODO: particles and all that
	// $mine.Explode(vectorSub($mine.to(%obj.getPosition()),(%data.miningRadius + 1) SPC (%data.miningRadius + 1) SPC (%data.miningRadius + 1)),%data.miningRadius);
}

datablock ItemData(MiningBombItem)
{
	candrop = true;
	uiName = "Explsoive";
	shapeFile = "./shapes/bomb.dts";
	image = "MiningBombImage";
};

datablock ShapeBaseImageData(MiningBombImage)
{
	className = "WeaponImage";

	item = "MiningBombItem";
	shapeFile = "./shapes/bomb.dts";
	armReady = true;

	projectile = "MiningBombProjectile";
   	projectileType = "Projectile";

	stateName[0] = "Activate";
	stateTimeoutValue[0] = 0;
	stateTransitionOnTimeout[0] = "Ready";

	stateName[1] = "Ready";
	stateTransitionOnTriggerDown[1] = "Charge";
	
	stateName[2] = "Charge";
	stateScript[2] = "onCharge";
	stateTimeoutValue[2] = 0.7;
	stateWaitForTimeout[2] = false;
	stateTransitionOnTriggerUp[2] = "CancelCharge";
	stateTransitionOnTimeout[2] = "Charged";

	stateName[3] = "Charged";
	stateSound[3] = "ArmBeepSound";
	stateTransitionOnTriggerUp[3] = "Fire";

	stateName[4] = "Fire";
	stateScript[4] = "onFire";
	stateTimeoutValue[4] = 0.5;
	stateFire[4] = true;
	stateTransitionOnTimeout[4]	= "Ready";

	stateName[5] = "CancelCharge";
	stateScript[5] = "onCancelCharge";
	stateTimeoutValue[5] = 0.01;
	stateTransitionOnTimeout[5] = "Ready";
};

function MiningBombImage::OnCharge(%db,%obj,%slot)
{
	%obj.playthread(2, "spearReady");
}

function MiningBombImage::OnCancelCharge(%db,%obj,%slot)
{
	%obj.playthread(2, "root");
}

function MiningBombImage::OnFire(%db,%obj,%slot)
{
	%obj.playthread(2, "spearThrow");

	%projectile = %db.projectile;
	%initPos = %obj.getMuzzlePoint(%slot);
	%muzzlevector = %obj.getMuzzleVector(%slot);

	if (%obj.isFirstPerson()) //are we close to an object?
	{
		%start = %obj.getEyePoint();
		%raycast = containerRayCast(%start, VectorAdd(%start, VectorScale(%obj.getEyeVector(), 5)), $TypeMasks::PlayerObjectType | $TypeMasks::VehicleObjectType | $TypeMasks::FxBrickObjectType 
		| $TypeMasks::StaticShapeObjectType | $TypeMasks::StaticObjectType, %obj, %obj.getObjectMount());
		if (%raycast)
		{
			if (VectorLen(VectorSub(getWords(%raycast,1,3), %start)) < 3.1)
			{
				%muzzlevector = %obj.getEyeVector ();
				%initPos = %start;
			}
		}
	}

	%muzzleVelocity = VectorAdd(VectorScale(%muzzlevector, VectorScale(%projectile.muzzleVelocity, getWord(%obj.getScale(), 2))), VectorScale(%obj.getVelocity(), %projectile.velInheritFactor));
	%p = new (%db.projectileType) ("")
	{
		dataBlock = %projectile;
		initialVelocity = %muzzleVelocity;
		initialPosition = %initPos;
		sourceObject = %obj;
		sourceSlot = %slot;
		client = %obj.client;
	};
	%p.setScale(%obj.getScale());
	%projectile.ArmLoop(%p);
}

//TODO: this is tempory mining
//TODO: replace $mine with a function or field to figure out what mine a person is in
package Mining
{
	function Armor::onTrigger(%db, %obj, %num, %down)
	{
		if(%obj.currtool < 0 && %num == 0 && %down)
		{
			%start = %obj.getEyePoint();
			%brick = ContainerRayCast(%start, VectorAdd(VectorScale(%obj.getEyeVector(),6),%Start), $TypeMasks::FxBrickObjectType);
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
					%item.setVelocity(vectorScale(vectorNormalize(getRandom() - 0.5 SPC getRandom() - 0.5 SPC getRandom() - 0.5),2));
					%item.schedulePop();
				}
				else
				{
					%obj.client.placableInventoryDirt++;
				}

				$mine.mineBrick[%pos] = true;
				%brick.delete();
				return;
			}
		}
		
		parent::onTrigger(%db, %obj, %num, %down);
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
		if (%col.getDataBlock().canRide && %this.rideAble && %this.nummountpoints > 0)
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

function makeMaterials()
{
	Material_Define("Dirt",8,"TTdirt01");
	Material_Define("Rock",7,"rockface");
	Material_Define("Gravel",7,"Old_Stone_Road");
	Material_Define("Dust",5,"whitesand");
	Material_Define("Iron",4,"brickTOP",true); // temporary texture
}
schedule(1000,0,"makeMaterials");

function test()
{
	%p = new Projectile()
	{
		dataBlock = MiningBombProjectile;
		initialVelocity = "0 0 0";
		initialPosition = "0 0 0";
	};
	%p.schedule(33,"explode");

	// $mine.delete();
	// $mine = mine_create("0 0 0.2",4,4,Material("Dirt"));
	// $mine.Lode(Material("Rock"),0,0.1,0.6);

	// $mine.RandomBlobs(Material("Iron"),328,10,0.5);

	// $mine.airCube("14 14 14",3,3);
	// $mine.revealCube("13 13 13",5,5);

	// ClientGroup.getObject(0).setMaxTools(7);
	// ClientGroup.getObject(0).player.clearTools();
	// ClientGroup.getObject(0).player.settransform($mine.From("15 15 15"));
	// ClientGroup.getObject(0).InventoryStack.push(Inventory("Base"));
}

function serverCmdCanColor(%client)
{
	%client.chatMessage(%client.player.getMountedImage(0).projectile.colorid);
}

function serverCmdPrintName(%client)
{
	%player = %client.player;
	%start = %player.getEyePoint();
	%brick = containerRayCast(%start, vectorAdd(vectorScale(%player.getEyeVector(),100),%start), $TypeMasks::FxBrickObjectType);
	if(%brick.getDatablock().hasPrint)
	{
		%file = getPrintTexture(%brick.printId);
		%client.chatMessage(getSubStr(%file,14,striPos(%file,"_",14) - 14) @ "/" @ filebase(%file));  // 14 being the length of Add-Ons/Print_
	}
	
}

//assets
//make ore model and ore cube texture
//need explosive model

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

//TODO: add explosive item
//TODO: construction and concretea