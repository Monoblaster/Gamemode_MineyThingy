// a implementation of a static octree
$Octree::MinSize = 4;

function Octree_Create(%size,%corner)
{
	return new ScriptObject()
	{
		corner = VectorAdd(%corner, "0 0 0");
		cornerX = getWord(%corner,0) + 0;
		cornerY = getWord(%corner,1) + 0;
		cornerZ = getWord(%corner,2) + 0;
		size = %size;
		depth = mFloor(%size / $Octree::MinSize);
		class = "Octree";
	};
}

function Octree::OnRemove(%tree)
{
	for(%i = 0; %i < 8; %i++)
	{
		if(isObject(%tree.child[%i]))
		{
			%tree.child[%i].delete();
		}
	}
}

//does area a contain area b
function Octree_ContainsArea(%xa,%ya,%za,%sa,%xb,%yb,%zb,%sb)
{
	return %xb >= %xa && %yb >= %ya && %zb >= %za && %xb + %sb < %xa + %sa && %yb + %sb < %ya + %sa && %zb + %sb < %za + %sa;
}

//does area a contain point b
function Octree_ContainsPoint(%xa,%ya,%za,%sa,%xb,%yb,%zb)
{
	return %xb >= %xa && %yb >= %ya && %zb >= %za && %xb < %xa + %sa && %yb < %ya + %sa && %zb < %za + %sa;
}

function Octree::Insert(%tree,%a,%corner,%size)
{	
	if(%tree.depth > 0)
	{
		%x = getWord(%corner,0);
		%y = getWord(%corner,1);
		%z = getWord(%corner,2);
		%childSize = %tree.size / 2;

		for(%i = 0; %i < 8; %i++)
		{
			//does this child contain our new thing?
			%cornerX = (%i % 2) * %childSize + %tree.cornerX;
			%cornerY = (mFloor(%i / 2) % 2) * %childSize + %tree.cornerY;
			%cornerZ = mFloor(%i / (2 * 2)) * %childSize + %tree.cornerZ;
			if(!Octree_ContainsArea(%cornerX,%cornerY,%cornerZ,%childSize,%x,%y,%z,%size))
			{
				continue;
			}

			//does the child exist yet?
			if(!isObject(%tree.child[%i]))
			{
				%tree.child[%i] = Octree_Create(%childSize,%cornerX SPC %cornerY SPC %cornerZ);
				%tree.child[%i].depth = %tree.depth - 1;
			}

			%tree.child[%i].Insert(%a,%corner,%size);
			return "";
		}
	}

	//we have reached max depth or no the item doesn't fit inside of any of our children
	%tree.items = lTrim(%tree.Items TAB %corner SPC %size SPC %a);
}

//will return all things that contain this point
function Octree::SearchPoint(%tree,%x,%y,%z)
{
	for(%i = 0; %i < 8; %i++)
	{
		//does the child exist?
		if(!isObject(%tree.child[%i]))
		{
			continue;
		}
		
		%child = %tree.child[%i];
		//does it contain this point?
		if(!Octree_ContainsPoint(%child.cornerx,%child.cornery,%child.cornerz,%child.size,%x,%y,%z))
		{
			continue;
		}

		//child contains point
		%list = %child.searchPoint(%x,%y,%z);
		break;
	}

	//find things that contain the point
	%things = %tree.items;
	%count = getFieldCount(%things);
	for(%i = 0; %i < %count; %i++)
	{
		%item = getField(%things,%i);
		if(!Octree_ContainsPoint(getWord(%item,0),getWord(%item,1),getWord(%item,2),getWord(%item,3),%x,%y,%z))
		{
			continue;
		}

		%list = %list SPC getWord(%item,4);
	}

	return lTrim(%list);
}