if(!isObject($Material::Set))
{
	$Material::Set = new SimSet();
}

function Material_Define(%name,%colorid,%printName,%drop)
{
	if(isObject(%name @ "Material"))
	{
		(%name @ "Material").delete();
	}

	%printName = "ModTer/" @ %printName;

	new ScriptObject(%name @ "Material")
	{
		class = "Material";
		name = %name;
		colorId = %colorId;
		printName = %printName;
		printId = $printNameTable[%printName];
		drop = %drop;
	};

	return %name @ "Material";
}

function Material(%name)
{
	if(isObject(%name @ "Material"))
	{
		return %name @ "Material";
	}

	return "";
}

function Material::OnAdd(%obj)
{
	$Material::Set.add(%obj);

	if(%obj.drop)
	{
		eval(
			"datablock ItemData("@%obj.getName()@"Item)"@
			"{"@
				"canDrop = true;"@
				"mass = 1;"@
				"density = 0.2;"@
				"elasticity = 0.4;"@
				"friction = 0.6;"@
				"uiName = %obj.name;"@
				"image = %obj.getName()@\"Image\";"@
				"doColorShift = true;"@
				"colorShiftColor = getColorIDTable(%obj.colorId);"@
				"shapeFile = \"add-ons/gamemode_mineythingy/ore.dts\";"@
			"};"@
			"datablock ShapeBaseImageData("@%obj.getName()@"Image)"@
			"{"@
				"className = \"OreImage\";"@
				"item = %obj.getName()@\"Item\";"@
				"doColorShift = true;"@
				"colorShiftColor = getColorIDTable(%obj.colorId);"@
				"shapeFile = \"add-ons/gamemode_mineythingy/ore.dts\";"@
				"offset = \"-0.5 0.3 0\";"@
				"eyeoffset = \"0 0.8 -0.8\";"@
			"};"
		);
	}
}

function OreImage::OnMount(%db,%obj,%slot)
{
	%obj.playThread(1, "armReadyBoth");
}

function OreImage::OnUnmount(%db,%obj,%slot)
{
	%obj.playThread(1, "root");
}