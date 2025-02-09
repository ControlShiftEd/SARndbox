/***********************************************************************
SurfaceAddWaterColor - Shader fragment to modify the base color of a
surface if the current fragment is under water.
Copyright (c) 2012 Oliver Kreylos
This file is part of the Augmented Reality Sandbox (SARndbox).
The Augmented Reality Sandbox is free software; you can redistribute it
and/or modify it under the terms of the GNU General Public License as
published by the Free Software Foundation; either version 2 of the
License, or (at your option) any later version.
The Augmented Reality Sandbox is distributed in the hope that it will be
useful, but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
General Public License for more details.
You should have received a copy of the GNU General Public License along
with the Augmented Reality Sandbox; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
***********************************************************************/

#extension GL_ARB_texture_rectangle : enable

/**********************************************************************
Helper functions to calculate 3D simplex Perlin noise. Code from Ian
McEwan, David Sheets, Stefan Gustavson, and Mark Richardson, according
to their 2012 JGT paper. Code included under MIT license.
**********************************************************************/

//Erosion and sedimentation visualization:
//Modified by Daniel Garcia-Castellanos (danielgc@ictja.csic.es) & Samadrita Karmakar (sam90_karmakar@yahoo.co.in)
//From ICTJA-CSIC (Barcelona)


vec3 mod289(vec3 x) {
  return x - floor(x * (1.0 / 289.0)) * 289.0;
}

vec4 mod289(vec4 x) {
  return x - floor(x * (1.0 / 289.0)) * 289.0;
}

vec4 permute(vec4 x) {
     return mod289(((x*34.0)+1.0)*x);
}

vec4 taylorInvSqrt(vec4 r)
{
  return 1.79284291400159 - 0.85373472095314 * r;
}

float snoise(vec3 v)
  {
  const vec2 C = vec2(1.0/6.0, 1.0/3.0) ;
  const vec4 D = vec4(0.0, 0.5, 1.0, 2.0);

// First corner
  vec3 i = floor(v + dot(v, C.yyy) );
  vec3 x0 = v - i + dot(i, C.xxx) ;

// Other corners
  vec3 g = step(x0.yzx, x0.xyz);
  vec3 l = 1.0 - g;
  vec3 i1 = min( g.xyz, l.zxy );
  vec3 i2 = max( g.xyz, l.zxy );

  // x0 = x0 - 0.0 + 0.0 * C.xxx;
  // x1 = x0 - i1 + 1.0 * C.xxx;
  // x2 = x0 - i2 + 2.0 * C.xxx;
  // x3 = x0 - 1.0 + 3.0 * C.xxx;
  vec3 x1 = x0 - i1 + C.xxx;
  vec3 x2 = x0 - i2 + C.yyy; // 2.0*C.x = 1/3 = C.y
  vec3 x3 = x0 - D.yyy; // -1.0+3.0*C.x = -0.5 = -D.y

// Permutations
  i = mod289(i);
  vec4 p = permute( permute( permute(
             i.z + vec4(0.0, i1.z, i2.z, 1.0 ))
           + i.y + vec4(0.0, i1.y, i2.y, 1.0 ))
           + i.x + vec4(0.0, i1.x, i2.x, 1.0 ));

// Gradients: 7x7 points over a square, mapped onto an octahedron.
// The ring size 17*17 = 289 is close to a multiple of 49 (49*6 = 294)
  float n_ = 0.142857142857; // 1.0/7.0
  vec3 ns = n_ * D.wyz - D.xzx;

  vec4 j = p - 49.0 * floor(p * ns.z * ns.z); // mod(p,7*7)

  vec4 x_ = floor(j * ns.z);
  vec4 y_ = floor(j - 7.0 * x_ ); // mod(j,N)

  vec4 x = x_ *ns.x + ns.yyyy;
  vec4 y = y_ *ns.x + ns.yyyy;
  vec4 h = 1.0 - abs(x) - abs(y);

  vec4 b0 = vec4( x.xy, y.xy );
  vec4 b1 = vec4( x.zw, y.zw );

  //vec4 s0 = vec4(lessThan(b0,0.0))*2.0 - 1.0;
  //vec4 s1 = vec4(lessThan(b1,0.0))*2.0 - 1.0;
  vec4 s0 = floor(b0)*2.0 + 1.0;
  vec4 s1 = floor(b1)*2.0 + 1.0;
  vec4 sh = -step(h, vec4(0.0));

  vec4 a0 = b0.xzyw + s0.xzyw*sh.xxyy ;
  vec4 a1 = b1.xzyw + s1.xzyw*sh.zzww ;

  vec3 p0 = vec3(a0.xy,h.x);
  vec3 p1 = vec3(a0.zw,h.y);
  vec3 p2 = vec3(a1.xy,h.z);
  vec3 p3 = vec3(a1.zw,h.w);

//Normalise gradients
  vec4 norm = taylorInvSqrt(vec4(dot(p0,p0), dot(p1,p1), dot(p2, p2), dot(p3,p3)));
  p0 *= norm.x;
  p1 *= norm.y;
  p2 *= norm.z;
  p3 *= norm.w;

// Mix final noise value
  vec4 m = max(0.6 - vec4(dot(x0,x0), dot(x1,x1), dot(x2,x2), dot(x3,x3)), 0.0);
  m = m * m;
  return 42.0 * dot( m*m, vec4( dot(p0,x0), dot(p1,x1),
                                dot(p2,x2), dot(p3,x3) ) );
  }

/**********************************************************
Helper function to calculate turbulence, i.e., 1/f |noise|:
**********************************************************/

float turb(in vec3 pos)
	{
	float result=0.0;
	result+=abs(snoise(pos));
	result+=abs(snoise(pos*2.0)/2.0);
	result+=abs(snoise(pos*4.0)/4.0);
	result+=abs(snoise(pos*8.0)/8.0);
	result+=abs(snoise(pos*16.0)/16.0);
	result+=abs(snoise(pos*32.0)/32.0);
	return result;
	}

/**********************
Water shading function:
**********************/

uniform sampler2DRect bathymetrySampler;
uniform sampler2DRect quantitySampler;
uniform float waterOpacity;
uniform float waterAnimationTime;

varying vec2 waterTexCoord; // Texture coordinate for water level texture

/***********************************************************************
Water shading function using a one-component water level texture and
fixed texture coordinates:
***********************************************************************/

void addWaterColor(in vec2 fragCoord,inout vec4 baseColor)
	{
	/* Calculate the water column height above this fragment: .r is top and b is bottom*/
	float b=(texture2DRect(bathymetrySampler,vec2(waterTexCoord.x-1.0,waterTexCoord.y-1.0)).r+
	         texture2DRect(bathymetrySampler,vec2(waterTexCoord.x,waterTexCoord.y-1.0)).r+
	         texture2DRect(bathymetrySampler,vec2(waterTexCoord.x-1.0,waterTexCoord.y)).r+
	         texture2DRect(bathymetrySampler,waterTexCoord.xy).r)*0.25;
	float waterLevel=texture2DRect(quantitySampler,waterTexCoord).r-b;//water column depth
	
	
	
	/* Check if the surface is under water: */
	if(waterLevel>0.0)
		{
		/* Calculate the water color based on velocity*depth (momentum): */		
		//Added by Samadrita Karmakar (sam90_karmakar@yahoo.co.in) and DGC
        //Linear Interpolation of Color
		float vx=((texture2DRect(quantitySampler,waterTexCoord).g))/waterLevel;
        float vy=((texture2DRect(quantitySampler,waterTexCoord).b))/waterLevel;
		//float energy=(vx*vx+vy*vy)*waterLevel; //ENERGY color criterion
		float energy=sqrt(vx*vx+vy*vy)*waterLevel; //MOMENTUM color criterion
		float min_energy_sedi=.28; //Wwater color is sedim below this value; starts to change to normal
		float max_energy_sedi=.3; //Lower value for normal water color
		float min_energy_eros=2.5; //Upper value for normal color; starts to change to erosion
		float max_energy_eros=3.0; //Water color is erosion above this value

		/*Set a vector with the 3 color components*/
		vec3 watcol;
        /*Default colors for intermediate (normal) energy range*/
		watcol.r=0; 
		watcol.g=0;
        watcol.b=1;
		/*How much color change out of the normal range:*/
		float bsed=.2, bero=.6; //0 means do not differenciate - 1 means pure color
		
		//linear color change for sedimentation 
		if (energy<max_energy_sedi) {
			float fac=(energy-min_energy_sedi)/(max_energy_sedi-min_energy_sedi);
			if (energy<min_energy_sedi) fac=0;
			watcol.g=bsed*(1-fac);
			watcol.b=1-bsed*5*(1-fac);
		}
		//linear color change for erosion
		if (energy>min_energy_eros) {
			float fac=(energy-min_energy_eros)/(max_energy_eros-min_energy_eros);
			if (energy>max_energy_eros) fac=1;
			watcol.r=bero*fac;
			watcol.b=1-bero*5*fac;
		}

		/*Compute slope vector for glittering*/
		vec3 wn=normalize(vec3(texture2DRect(quantitySampler,vec2(waterTexCoord.x-1.0,waterTexCoord.y)).r-
		                       texture2DRect(quantitySampler,vec2(waterTexCoord.x+1.0,waterTexCoord.y)).r,
		                       texture2DRect(quantitySampler,vec2(waterTexCoord.x,waterTexCoord.y-1.0)).r-
		                       texture2DRect(quantitySampler,vec2(waterTexCoord.x,waterTexCoord.y+1.0)).r,
		                       0.25));
        //Water Glittering color factor from 0 to 1:
		float colorG=pow(dot(wn,normalize(vec3(0.075,0.075,1.0))),100.0)*.5+0.;
		//vec4 waterColor=vec4(colorG,colorG,1.0,1.0); // Water
		//vec4 waterColor=vec4(1.0-colorG,1.0-colorG*2.0,0.0,1.0); // Lava
		vec4 waterColor=vec4(colorG+watcol.r,colorG+watcol.g,colorG+watcol.b,1.0);
		/*Opacity: Mix the water color with the base surface color based on the water level:*/
		baseColor=mix(baseColor,waterColor,min(waterLevel*waterOpacity*20,1.0));
		}
	}

/***********************************************************************
Water shading function using a three-component water level texture
containing the water level, and 2D noise coordinates from texture
advection:
***********************************************************************/

void addWaterColorAdvected(inout vec4 baseColor)
	{
	#if 0
	/* Check if the surface is under water: */
	vec3 waterLevelTex=texture2DRect(waterLevelSampler,waterTexCoord).rgb;
	if(waterLevelTex.b>=1.0/2048.0)
		{
		/* Calculate the water color: */
		// float colorW=max(snoise(vec3(waterLevelTex.rg*0.05/waterLevelTex.b,waterAnimationTime*0.25)),0.0); // Simple noise function
		float colorW=max(turb(vec3(waterLevelTex.rg*0.05/waterLevelTex.b,waterAnimationTime*0.25)),0.0); // Turbulence noise
		
		//vec3 noisePos=vec3(waterLevelTex.rg*0.045/waterLevelTex.b,waterAnimationTime*0.25);
		//vec3 noiseNormal=normalize(vec3(snoise(noisePos-vec3(0.001,0.0,0.0))-snoise(noisePos+vec3(0.001,0.0,0.0)),
		//                                snoise(noisePos-vec3(0.0,0.001,0.0))-snoise(noisePos+vec3(0.0,0.001,0.0)),
		//                                0.0025));
		//float colorW=1.0-pow(noiseNormal.z,2.0);
		
		vec4 waterColor=vec4(1.0-colorW,1.0-colorW,1.0,1.0); // Water
		//vec4 waterColor=vec4(1.0-colorW,1.0-colorW*2.0,0.0,1.0); // Lava
		
		/* Mix the water color with the base surface color based on the water level: */
		baseColor=mix(baseColor,waterColor,min(waterLevelTex.b*waterOpacity,1.0));
		}
	#endif
	}
