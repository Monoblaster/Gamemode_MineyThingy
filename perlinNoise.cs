function PerlinNoise_Create(%seed)
{
	return new ScriptObject(){class = "PerlinNoise"; seed = %seed;};
}

function PerlinNoise::OnAdd(%obj)
{
	//generate the permutation table
	for(%i = 0; %i < 256; %i++)
	{
		%obj.p[%i] = %i;
	}

	if(%obj.seed !$= "")
	{
		setRandomSeed(%obj.seed);
	}
	//shuffle the table
	for(%i = 0; %i < 256; %i++)
	{
		%r = getRandom(%i,255);
		%temp = %obj.p[%i];
		%obj.p[%i] = %obj.p[%r];
		%obj.p[%r] = %temp;
	}

	for(%i = 0; %i < 256; %i++)
	{
		%obj.p[%i + 256] = %obj.p[%i];
	}
}

function PerlinNoise_Gradient(%hash,%x,%y,%z)
{
	%hash = %hash & 15;
	%u = %hash < 8 ? %x : %y;
	%v = %hash < 4 ? %y : (%hash == 12 || %hash == 14 ? %x : %z);
	return ((%hash & 1) == 0 ? %u : -%u) + ((%hash & 2) == 0 ? %v : -%v);
}

function PerlinNoise_Lerp(%a,%b,%x)
{
	return %a + %x * (%b - %a);
}

function PerlinNoise::Sample(%obj,%x,%y,%z)
{
	%xi = %x & 255; //x y and z index of the cube
	%yi = %y & 255;
	%zi = %z & 255;

	%x -= mFloor(%x); //x y and z in cube space
	%y -= mFloor(%y);
	%z -= mFloor(%z);

	%u = %x * %x * %x * (%x * (%x * 6 - 15) + 10); //x y and z put through a smoothing function
	%v = %y * %y * %y * (%y * (%y * 6 - 15) + 10);
	%w = %z  * %z * %z * (%z * (%z * 6 - 15) + 10);

	return //TODO: this should probably be unwrapped for the sake of perfomance but that sounds painful!!!
	(PerlinNoise_Lerp(
		PerlinNoise_Lerp(
			PerlinNoise_Lerp(
				PerlinNoise_Gradient(%obj.p[%obj.p[%obj.p[%xi]+ %yi]+ %zi], %x, %y, %z),
				PerlinNoise_Gradient(%obj.p[%obj.p[%obj.p[%xi]+ %yi+1]+ %zi], %x-1, %y, %z),
				%u),
			PerlinNoise_Lerp(
				PerlinNoise_Gradient(%obj.p[%obj.p[%obj.p[%xi]+ %yi]+ %zi+1], %x, %y-1, %z),
				PerlinNoise_Gradient(%obj.p[%obj.p[%obj.p[%xi]+ %yi+1]+ %zi+1], %x-1, %y-1, %z), %u),
				%v),
		PerlinNoise_Lerp(
			PerlinNoise_Lerp(
				PerlinNoise_Gradient(%obj.p[%obj.p[%obj.p[%xi+1]+ %yi]+ %zi], %x, %y, %z-1),
				PerlinNoise_Gradient(%obj.p[%obj.p[%obj.p[%xi+1]+ %yi+1]+ %zi], %x-1, %y, %z-1),
				%u),
			PerlinNoise_Lerp(
				PerlinNoise_Gradient(%obj.p[%obj.p[%obj.p[%xi+1]+ %yi]+ %zi+1], %x, %y-1, %z-1),
				PerlinNoise_Gradient(%obj.p[%obj.p[%obj.p[%xi+1]+ %yi+1]+ %zi+1], %x-1, %y-1, %z-1), %u),
				%v),
	%w) + 1) / 2;
}
