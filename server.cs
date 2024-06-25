exec("./perlinNoise.cs");
exec("./octree.cs");
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
		if(%brick.isMineable)
		{
			$mine.revealArea(vectorSub($mine.To(%brick.getPosition()),"1 1 1"),3,3);
			%brick.delete();
			return;
		}
		
		parent::onActivate(%brick);
	}
};
activatePackage("Mining");

Material_Define("Dirt",8,2);
Material_Define("Rock",7,2);
Material_Define("Iron",4,2,true);

function test()
{
	$mine.delete();
	$mine = mine_create("0 0 0.2",10,2,Material("Dirt"));
	$mine.Lode(Material("Rock"),0,0.1,0.6);

	$mine.RandomBlobs(Material("Iron"),5000,10,0.5);

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
// maybe you can get dirt and rocks? i dunno probably just decoration
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