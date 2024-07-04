// INVENTORY
if(!isObject($InventoryUI::Set))
{
	$inventoryUI::Set = new SimSet();
}

function Inventory(%name)
{
	if(isObject(%name SPC "Inventory"))
	{
		return (%name SPC "Inventory").getId();
	}

	return new ScriptObject(%name SPC "Inventory")
	{
		class = "Inventory";
		name = %name;
	};
}

function Inventory::OnAdd(%obj)
{
	$inventoryUI::Set.add(%obj);
}

$Inventory::Empty = Inventory("Empty");

function Inventory::Set(%inv,%slot,%db)
{
	%inv.tool[%slot] = %db;

	return %inv;
}

function Inventory::Get(%inv,%slot)
{
	return %inv.tool[%slot];
}

function Inventory::Display(%inv,%client,%writeBlank,%offset)
{
	if(!isObject(%client) || %client.getClassName() !$= "GameConnection")
	{
		return %inv;
	}

	for(%i = 0; %i < 20; %i++)
	{
		%tool = %inv.tool[%i + %offset];
		if(!isObject(%tool) && !%writeBlank)
		{
			continue;
		}

		if(isObject(%tool))
		{
			%tool = %tool.getId();
		}

		messageClient(%client,'MsgItemPickup',"",%i,%tool,1);
	}
}

// INVENTORY STACK
function InventoryStack::Push(%stack,%inv)
{
	%stack.active = %inv;
	if(!%inv.dontAutoOpen)
	{
		commandToClient(%stack.client,'SetActiveTool',0);
	}
	%stack.set.add(%inv);
	%stack.set.bringToFront(%inv);
	call(%inv.push,%stack);
	%stack.display();	
}

function InventoryStack::Pop(%stack)
{
	%first = %stack.set.getObject(0);
	%stack.set.pushToBack(%first);
	%stack.set.remove(%first);
	%stack.active = "";
	if(%stack.set.getCount() > 0)
	{
		%stack.active = %stack.set.getObject(0);
	}

	if(!%stack.active.dontAutoOpen)
	{
		commandToClient(%stack.client,'SetActiveTool',0);
	}
	%stack.display();
	call(%inv.pop,%stack);
}

function InventoryStack::Clear(%stack)
{
	%stack.active = "";
	%stack.set.delete();
	%stack.set = new SimSet();
}

function InventoryStack::Peek(%stack,%n)
{
	return %stack.set.GetObject(%n + 0);
}

function InventoryStack::Print(%stack)
{
	%curr = %stack.active;
	%slot = %stack.client.currtool;
	if(%curr $= "")
	{
		return;
	}

	if(!%curr.cantClose)
	{
		%controls = "Close to cancel";
	}

	if(%slot != -1 && isObject(%curr.tool[%slot]) || !%curr.dontOverwrite)
	{
		%controls = "\c4Click to use" NL %controls;
	}
	%stack.client.centerPrint(trim(call(%curr.select,%stack,%slot) NL %controls));
}

function InventoryStack::Display(%stack)
{
	%client = %stack.client;
	%player = %client.player;
	%client.centerPrint("");
	%curr = %stack.active;

	if(%curr $= "")
	{
		if(isObject(%player))
		{
			Inventory::Display(%player,%client,true);
			return;
		}
		Inventory::Display($Inventory::Empty,%client,true);
		return;
	}

	if(isFunction(%curr.display))
	{
		call(%curr.display,%stack,%client.currTool);
		return;
	}
	
	if(%curr.canUseTools && isObject(%player))
	{
		serverCmdUseTool(%client,%player.currTool);
	}
	else
	{
		%player.currTool = -1;
		%player.unmountImage(0);
		%player.playThread (1, root);
	}

	%curr.display(%client,!%curr.dontOverwrite);
}

package InventoryStack
{
	function GameConnection::onClientEnterGame(%client)
	{
		%obj = new ScriptObject()
		{
			class = "InventoryStack";
			client = %client;
			active = "";
		};
		%obj.set = new SimSet();
		%client.InventoryStack = %obj;
		return parent::onClientEnterGame(%client);
	}

	function GameConnection::onDrop(%client, %reason)
	{
		%client.InventoryStack.set.delete();
		%client.InventoryStack.delete();
		return parent::onDrop(%client,%reason);
	}

	function serverCmdUnUseTool(%client)
	{
		if(%client.InventoryStack.active !$= "")
		{
			%client.centerPrint("");
			if(!%client.InventoryStack.active.cantClose)
			{
				%client.InventoryStack.pop();
			}
		}
		
		return Parent::serverCmdUnUseTool(%client);
	}

	function serverCmdUseTool(%client,%slot)
	{
		%curr = %client.InventoryStack.active;
		if(%curr !$= "")
		{
			if(!%curr.cantClose)
			{
				%controls = "Close to cancel";
			}

			if(%slot != -1 && isObject(%curr.tool[%slot]) || !%curr.dontOverwrite)
			{
				%client.currTool = %slot;
				%client.player.currTool = %slot;
				%controls = "\c4Click to use" NL %controls;
				%slot = -1;
			}
			%client.centerPrint(trim(call(%curr.select,%client.InventoryStack,%client.currTool) NL %controls));

			%player = %client.player;
			if(%slot == -1 && isobject(%player))
			{
				%player.unmountImage(0);
				fixArmReady(%player);
			}

			if(!%curr.canUseTools)
			{
				return;
			}
		}

		return parent::serverCmdUseTool(%client,%slot);
	}

	function Observer::onTrigger(%db, %obj, %num, %down)
	{
		%client = %obj.getControllingClient();
		if(isObject(%client) && %num == 0 && %down)
		{
			if(%client.currTool >= 0 && %client.InventoryStack.active !$= "")
			{
				call(%client.InventoryStack.active.use,%client.InventoryStack,%client.currTool);
				return;
			}
		}
		return Parent::onTrigger(%db, %obj, %num, %down);
	}

	function Armor::onTrigger(%db, %obj, %num, %down) 
	{
		%client = %obj.getControllingClient();
		if(isObject(%client) && %num == 0 && %down)
		{
			if(%client.currTool >= 0 && %client.InventoryStack.active !$= "")
			{
				call(%client.InventoryStack.active.use,%client.InventoryStack,%client.currTool);
				return;
			}
		}
		return Parent::onTrigger(%db, %obj, %num, %down);
	}
};
activatepackage("InventoryStack");

// MAX TOOLS
function GameConnection::setMaxTools(%c,%n)
{
	if (%numSlots < 0 || %numSlots > 30)
	{
		return;
	}
	%c.maxTools = %n;
	commandtoclient(%c,'PlayGui_CreateToolHud',%n);
	if(%c.currTool > -1)
	{
		commandToClient(%c,'SetActiveTool',%c.currTool);
	}
}

function GameConnection::clearMaxTools(%c)
{
	%player = %c.player;
	if(isObject(%player))
	{
		commandtoclient(%c,'PlayGui_CreateToolHud',%player.getDataBlock().maxtools);
		if(%c.currTool > -1)
		{
			commandToClient(%c,'SetActiveTool',%c.currTool);
		}
	}
}

function GameConnection::getMaxTools(%client)
{
	%player = %client.player;
	%maxTools = %client.maxTools;
	if(isObject(%player) && %maxTools $= "")
	{
		%maxTools = %player.getDataBlock().maxTools;
	}
	return %maxTools;
}

package InventoryUtil
{
	function serverCmdUnUseTool(%client)
	{
		%client.currTool = -1;
		return Parent::serverCmdUnUseTool(%client);
	}

	function serverCmdUseTool(%client,%slot)
	{
		%client.currTool = %slot;
		return parent::serverCmdUseTool(%client,%slot);
	}

	function GameConnection::createPlayer (%client, %spawnPoint)
	{
		%r = parent::createPlayer (%client, %spawnPoint);
		commandtoclient(%client,'PlayGui_CreateToolHud',%client.getMaxTools());
		return %r;
	}
};
activatepackage("InventoryUtil");

//code overwrites to make maxtools work
function Player::ClearTools (%player)
{
	%client = %player.client;
	%maxTools = %client.getMaxTools();
	%i = 0;
	while (%i < %maxTools)
	{
		%player.tool[%i] = 0;
		if (isObject (%client))
		{
			messageClient (%client, 'MsgItemPickup', "", %i, 0, 1);
		}
		%i += 1;
	}
	%player.unmountImage (0);
}

function ItemData::onPickup (%this, %obj, %user, %amount)
{
	if (%obj.canPickup == 0)
	{
		return;
	}
	%player = %user;
	%client = %player.client;
	%data = %player.getDataBlock ();
	if (!isObject (%client))
	{
		return;
	}
	%mg = %client.miniGame;
	if (isObject (%mg))
	{
		if (%mg.WeaponDamage == 1)
		{
			if (getSimTime () - %client.lastF8Time < 5000)
			{
				return;
			}
		}
	}
	%canUse = 1;
	if (miniGameCanUse (%player, %obj) == 1)
	{
		%canUse = 1;
	}
	if (miniGameCanUse (%player, %obj) == 0)
	{
		%canUse = 0;
	}
	if (!%canUse)
	{
		if (isObject (%obj.spawnBrick))
		{
			%ownerName = %obj.spawnBrick.getGroup ().name;
		}
		%msg = %ownerName @ " does not trust you enough to use this item.";
		if ($lastError == $LastError::Trust)
		{
			%msg = %ownerName @ " does not trust you enough to use this item.";
		}
		else if ($lastError == $LastError::MiniGameDifferent)
		{
			if (isObject (%client.miniGame))
			{
				%msg = "This item is not part of the mini-game.";
			}
			else 
			{
				%msg = "This item is part of a mini-game.";
			}
		}
		else if ($lastError == $LastError::MiniGameNotYours)
		{
			%msg = "You do not own this item.";
		}
		else if ($lastError == $LastError::NotInMiniGame)
		{
			%msg = "This item is not part of the mini-game.";
		}
		commandToClient (%client, 'CenterPrint', %msg, 1);
		return;
	}
	%maxTools = %client.maxTools;
	if(%maxTools $= "")
	{
		%maxTools = %player.getDataBlock().maxTools;
	}
	%freeslot = -1;
	%i = 0;
	while (%i < %maxTools)
	{
		if (%player.tool[%i] == 0)
		{
			%freeslot = %i;
			break;
		}
		%i += 1;
	}
	if (%freeslot != -1)
	{
		if (%obj.isStatic ())
		{
			%obj.Respawn ();
		}
		else 
		{
			%obj.delete ();
		}
		%player.tool[%freeslot] = %this;
		if (%user.client)
		{
			if(%user.client.InventoryStack.active $= "" || %user.client.InventoryStack.active.showNewItems)
			{
				messageClient (%user.client, 'MsgItemPickup', '', %freeslot, %this.getId ());
			}
			else
			{
				messageClient (%user.client, 'MsgItemPickup', '', -1, ""); // to play the pickup sound lol
			}
		}
		return 1;
	}
}

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
		%maxTools = %client.getMaxTools();
		// %i = 0; //TODO: TOgGLE THIS
		// while (%i < %maxTools )
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