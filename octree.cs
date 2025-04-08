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

function Octree::OnAdd(%tree)
{
	%tree.itemCount = 0;
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
	%tree.itemXA[%tree.itemCount] = getWord(%corner,0);
	%tree.itemYA[%tree.itemCount] = getWord(%corner,1);
	%tree.itemZA[%tree.itemCount] = getWord(%corner,2);
	%tree.itemXB[%tree.itemCount] = getWord(%corner,0) + %size;
	%tree.itemYB[%tree.itemCount] = getWord(%corner,1) + %size;
	%tree.itemZB[%tree.itemCount] = getWord(%corner,2) + %size;
	%tree.itemThing[%tree.itemCount] = %a;
	%tree.itemCount++;
}

//will return all things that contain this point
function Octree::SearchPoint(%nextChild,%x,%y,%z)
{
	%pos = %x SPC %y SPC %z;
	while(%nextChild !$= "")
	{
		%currTree = %nextChild;
		%nextChild = "";

		%count = %currTree.ItemCount;
		for(%i = 0; %i < %count; %i++)
		{
			if(%x >= %currtree.itemXA[%i] && %y >= %currtree.itemYA[%i] && %z >= %currtree.itemZA[%i] && %x < %currtree.itemXB[%i] && %y < %currtree.itemYB[%i] && %z < %currtree.itemZB[%i])
			{
				%list = %list SPC %currtree.itemThing[%i];
			}
		}

		for(%i = 0; %i < 8; %i++)
		{
			%child = %currTree.child[%i];
			//does the child exist?
			if(%child !$= "" && %x >= %child.CornerX && %y >= %child.CornerY && %z >= %child.CornerZ && %x < %child.CornerX + %child.size && %y < %child.CornerY + %child.size && %z < %child.CornerZ + %child.size)
			{
				%nextChild = %child;
				break;
			}
		}
	}

	return lTrim(%list);
}

//will return all things that intersect with this area
function Octree::SearchArea(%nextChild,%xa,%ya,%za,%size)
{
	%xb = %xa + %size;
	%yb = %ya + %size;
	%zb = %za + %size;
	while(%nextChild !$= "")
	{
		%currTree = %nextChild;
		%nextChild = "";

		%count = %currTree.ItemCount;
		for(%i = 0; %i < %count; %i++)
		{
			if(%xb >= %currtree.itemXA[%i] && %xa < %currtree.itemXB[%i] && %yb >= %currtree.itemYA[%i] && %ya < %currtree.itemYA[%i] && %zb >= %currtree.itemZA[%i] && %za < %currtree.itemZA[%i])
			{
				%list = %list SPC %currtree.itemThing[%i];
			}
		}

		for(%i = 0; %i < 8; %i++)
		{
			%child = %currTree.child[%i];
			//does the child exist?
			if(%child !$= "" && %xb >= %child.CornerX && %xa < %child.CornerX + %child.size && %yb >= %child.CornerY && %ya < %child.CornerY + %child.size && %zb >= %child.CornerZ && %za < %child.CornerZ + %child.size)
			{
				%nextChild = %child;
				break;
			}
		}
	}

	return lTrim(%list);
}