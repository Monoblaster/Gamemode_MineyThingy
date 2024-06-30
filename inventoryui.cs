if(!isObject($InventoryUI))
{
	$inventoryUI = new ScriptObject(){class = "InventoryUI";};
}

function InventoryUI()
{
	return $inventoryUI;
}

function InventoryUI::add(%obj, %name)
{
	talk(%obj.UI[%name]);
	if(%obj.UI[%name] !$= "")
	{
		%obj.UI[%name].delete();
	}

	%inv = Inventory_Create();
	%obj.UI[%name] = %inv;
	%obj.list = ltrim(%obj.list SPC %inv);
	return %inv;
}

function InventoryUI::get(%obj, %name)
{
	return %obj.UI[%name];
}

function InventoryUI::onRemove(%obj)
{
	%s = %obj.list;
	%count = getWordCount(%s);
	for(%i = 0; %i < %count; %i++)
	{
		getWord(%s,%i).delete();
	}
}

function GameConnection::InventoryUI_push(%client,%inv)
{
	commandToClient(%client,'SetActiveTool',0);
	%client.InventoryUI_stack = trim(%inv SPC %client.InventoryUI_stack);
	call($InventoryUI.get(%inv).push,%client,%inv,0);
	%client.InventoryUI_display();
}

function GameConnection::InventoryUI_pop(%client)
{
	commandToClient(%client,'SetActiveTool',0);
	%client.InventoryUI_stack = removeWord(%client.InventoryUI_stack,0);
	%client.InventoryUI_display();
	call($InventoryUI.get(%inv).pop,%client,%inv,0);
}

function GameConnection::InventoryUI_clear(%client)
{
	%client.InventoryUI_stack = "";
	%client.InventoryUI_display();
}

function GameConnection::InventoryUI_peek(%client,%n)
{
	return $InventoryUI.get(getWord(%client.InventoryUI_stack,%n));
}

function GameConnection::InventoryUI_top(%client)
{
	return $InventoryUI.get(firstWord(%client.InventoryUI_stack));
}

function GameConnection::InventoryUI_display(%client)
{
	%client.centerPrint("");
	%curr = %client.InventoryUI_top();

	if(%curr $= "")
	{
		%client.currUi = "";
		%player = %client.player;
		if(isObject(%player))
		{
			Inventory::Display(%player,%client,true);
			return;
		}
		Inventory::Display(%client,%client,true);
		return;
	}

	call(%curr.display,%client,%curr,%client.currInvSlot);
	%curr.display(%client,!%curr.overlay);

	%player = %client.player;
	if(isObject(%player))
	{
		serverCmdUseTool(%client,%player.currTool);
		if(%curr.displayTools)
		{
			Inventory::Display(%player,%client);
		}

		if(!%curr.canUseItems)
		{
			%player.currTool = -1;
			%client.currInv = -1;
			%client.currInvSlot = -1;
			%player.unmountImage(0);
			%player.playThread (1, root);
		}
	}
}

package InventoryUI
{
	function serverCmdUnUseTool(%client)
	{
		%curr = %client.InventoryUI_top();
		if(%curr !$= "")
		{
			%client.currUi = "";
			if(!%curr.cantClose)
			{
				%client.InventoryUI_pop();
			}
		}
		
		return Parent::serverCmdUnUseTool(%client);
	}

	function serverCmdUseTool(%client,%slot)
	{
		%curr = %client.InventoryUI_top();
		
		if(%curr !$= "")
		{
			%client.currUi = "";
			if(%slot != -1 && isObject(%curr.tool[%slot]) || %curr.overlay)
			{
				%client.currUi = %slot;
			}
			%uislot = %client.currUi;

			if(%uislot !$= "")
			{
				%slot = -1;
				%controls = "\c4Click to use";
				if(!%curr.cantClose)
				{
					%controls = %controls NL "Close to cancel";
				}
			}
			%client.centerPrint(trim(call(%curr.print,%client,%curr,%uislot) NL %controls));

			if(!%curr.canUseItems)
			{
				return;
			}

			%player = %client.player;
			if(%slot == -1 && isobject(%player))
			{
				%player.unmountImage(0);
				fixArmReady(%player);
			}
		}

		return parent::serverCmdUseTool(%client,%slot);
	}

	function Player::ActivateStuff(%player)
	{
		%client = %player.client;
		if(isObject(%client))
		{
			%slot = %client.currUi;
			if(%slot !$= "")
			{
				%curr = %client.InventoryUI_top();
				%next = call(%curr.next,%client,%curr,%slot);
				if(%next !$= "")
				{
					if(%next $= "pop")
					{
						%client.InventoryUI_pop();
					}
					else
					{
						%client.InventoryUI_push(%next);
					}
				}
				call(%curr.use,%client,%curr,%slot);
				if(!%curr.canUseItems || !isObject(%player.tool[%slot]))
				{
					return;
				}
			}
		}
		return parent::ActivateStuff(%player);
	}
};
activatepackage("InventoryUI");