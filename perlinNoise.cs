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

function PerlinNoise::Sample(%obj,%x,%y,%z)
{
	%xi = %x & 255; //x y and z index of the cube
	%yi = %y & 255;
	%zi = %z & 255;

	%x -= %x | 0; //x y and z in cube space
	%y -= %y | 0;
	%z -= %z | 0;

	%hasha = %obj.p[%obj.p[%obj.p[%xi]+ %yi]+ %zi] & 15;
	%hashb = %obj.p[%obj.p[%obj.p[%xi]+ %yi+1]+ %zi] & 15;
	%hashc = %obj.p[%obj.p[%obj.p[%xi]+ %yi]+ %zi+1] & 15;
	%hashd = %obj.p[%obj.p[%obj.p[%xi]+ %yi+1]+ %zi+1] & 15;
	%hashe = %obj.p[%obj.p[%obj.p[%xi+1]+ %yi]+ %zi] & 15;
	%hashf = %obj.p[%obj.p[%obj.p[%xi+1]+ %yi+1]+ %zi] & 15;
	%hashg = %obj.p[%obj.p[%obj.p[%xi+1]+ %yi]+ %zi+1] & 15;
	%hashh = %obj.p[%obj.p[%obj.p[%xi+1]+ %yi+1]+ %zi+1] & 15;

	%a = ((%hasha & 1) == 0 ? 1 : -1) * (%hasha < 8 ? %x : %y) + ((%hasha & 2) == 0 ? 1 : -1) * (%hasha < 4 ? %y : (%hasha == 12 || %hasha == 14 ? %x : %z)) + (%x * %x * %x * (%x * (%x * 6 - 15) + 10)) *
		(((%hashb & 1) == 0 ? 1 : -1) * (%hashb < 8 ? %x-1 : %y) + ((%hashb & 2) == 0 ? 1 : -1) * (%hashb < 4 ? %y : (%hashb == 12 || %hashb == 14 ? %x-1 : %z)) + 
		((%hasha & 1) == 0 ? 1 : -1) * (%hasha < 8 ? %x : %y) + ((%hasha & 2) == 0 ? 1 : -1) * (%hasha < 4 ? %y : (%hasha == 12 || %hasha == 14 ? %x : %z)));

	%b = ((%hashc & 1) == 0 ? 1 : -1) * (%hashc < 8 ? %x : %y-1) + ((%hashc & 2) == 0 ? 1 : -1) * (%hashc < 4 ? %y-1 : (%hashc == 12 || %hashc == 14 ? %x : %z)) + (%x * %x * %x * (%x * (%x * 6 - 15) + 10)) *
		(((%hashd & 1) == 0 ? 1 : -1) * (%hashd < 8 ? %x-1 : %y-1) + ((%hashd & 2) == 0 ? 1 : -1) * (%hashd < 4 ? %y-1 : (%hashd == 12 || %hashd == 14 ? %x-1 : %z)) + 
		((%hashc & 1) == 0 ? 1 : -1) * (%hashc < 8 ? %x : %y-1) + ((%hashc & 2) == 0 ? 1 : -1) * (%hashc < 4 ? %y-1 : (%hashc == 12 || %hashc == 14 ? %x : %z)));
	
	%c = ((%hashe & 1) == 0 ? 1 : -1) * (%hashe < 8 ? %x : %y) + ((%hashe & 2) == 0 ? 1 : -1) * (%hashe < 4 ? %y : (%hashe == 12 || %hashe == 14 ? %x : %z-1)) + (%x * %x * %x * (%x * (%x * 6 - 15) + 10)) *
		(((%hashf & 1) == 0 ? 1 : -1) * (%hashf < 8 ? %x-1 : %y) + ((%hashf & 2) == 0 ? 1 : -1) * (%hashf < 4 ? %y : (%hashf == 12 || %hashf == 14 ? %x-1 : %z-1)) + 
		((%hashe & 1) == 0 ? 1 : -1) * (%hashe < 8 ? %x : %y) + ((%hashe & 2) == 0 ? 1 : -1) * (%hashe < 4 ? %y : (%hashe == 12 || %hashe == 14 ? %x : %z-1)));
	
	%d = ((%hashg & 1) == 0 ? 1 : -1) * (%hashg < 8 ? %x : %y-1) + ((%hashg & 2) == 0 ? 1 : -1) * (%hashg < 4 ? %y-1 : (%hashg == 12 || %hashg == 14 ? %x : %z-1)) + (%x * %x * %x * (%x * (%x * 6 - 15) + 10)) *
		(((%hashh & 1) == 0 ? 1 : -1) * (%hashh < 8 ? %x-1 : %y-1) + ((%hashh & 2) == 0 ? 1 : -1) * (%hashh < 4 ? %y-1 : (%hashh == 12 || %hashh == 14 ? %x-1 : %z-1)) + 
		((%hashg & 1) == 0 ? 1 : -1) * (%hashg < 8 ? %x : %y-1) + ((%hashg & 2) == 0 ? 1 : -1) * (%hashg < 4 ? %y-1 : (%hasha == 12 || %hashg == 14 ? %x : %z-1)));

	return (%a + (%y * %y * %y * (%y * (%y * 6 - 15) + 10)) * (%b - %a) + (%z  * %z * %z * (%z * (%z * 6 - 15) + 10)) * (%c + (%y * %y * %y * (%y * (%y * 6 - 15) + 10)) * (%c - %d) - %a + (%y * %y * %y * (%y * (%y * 6 - 15) + 10)) * (%b - %a)) + 1) / 2; // i hope this is faster lol
}
