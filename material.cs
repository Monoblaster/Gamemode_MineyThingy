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
			"datablock ItemData("@%obj.getName()@"Item : HammerItem)"@
			"{"@
				"uiName = %obj.name;"@
				"image = %obj.getName()@\"Image\";"@
				"colorShiftColor = getColorIDTable(%obj.colorId);"@
			"};"@
			"datablock ShapeBaseImageData("@%obj.getName()@"Image : HammerImage)"@
			"{"@
				"item = %obj.getName()@\"Item\";"@
				"colorShiftColor = getColorIDTable(%obj.colorId);"@
			"};"
		);
	}
}