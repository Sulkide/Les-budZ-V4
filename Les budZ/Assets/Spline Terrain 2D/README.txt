Spline Terrain 2D

2D Level design / Level Management Plugin package.

The package contains the plugin, 2 shaders and 2 standard materials

For full description read  the file FULL GUIDE.txt

QUICK START:

	1) Go to Tools / Kolibri2D / Create Data / Terrain2D Material
	2) Go to Tools / Kolibri2D / + Terrain2d
	3) Add the material to the Terrain2d component
	4) set your texture's WRAP MODE (on the texture inspector) to REPEAT 
	4) Set sprites and/or physics settings on the material
	5) Start creating your scenario

	
CREATING SPLINES:

	Tools / Kolibri2D / Spline (variants)
	
	* To add points in scene, your Scene View MUST BE IN 2D-VIEW and EDIT POINTS MODE enabled
	
	1- Click on EDIT to edit the spline points in the scene
	2- To create a point, click on scene when the guides are visible
	3- To delete a point select the point(s) and press [SUPR] or [DELETE]
		
		
CREATING MATERIALS:

	TERRAIN2D
	
		from menu:          Tools / Kolibri2D / Create Data / Terrain2D Material
		from Project Tab    Kolibri2D / Decoration Material
		
		Sprites used for terrain should set it's texture's WRAP MODE (on the texture inspector) to REPEAT  
	
		
		1- Drag your sprites into the correspondant slot (Ground, Wall, Ceiling of Fill)
		2- Under the Colliders Section, you can toggle physics settings for the terrain
	
	DECORATION MATERIAL
	
		from Menu:          Tools / Kolibri2D / Create Data / Decoration Material
		from Project Tab    Kolibri2D / Decoration Material
		
		1- Create new group
		2- Drag your assets in the corespondant slot ( Ground, Wall, Ceiling)
		
SUGGESTIONS:

	- Most of the SLIDERS just move between sugested values, but THIS DOES NOT represent the actual limits of the values.
	- I recomend to keep Decoration group as independent as possible, only containing sprites or prefabs related to each other.
		Dont create several similar groups, you can reuse them in other materials.
		The more unique the content of each group is, the more your productivity will increase.
	- I strongly suggest you to explore the full potential of Architecture angles, since it can increase drastically the usage of each material
	

Youtube
https://www.youtube.com/channel/UC8wXAnAbD-F5fi2aAydz9cA

Facebook
@kolibri2dPlugin		https://www.facebook.com/Kolibri2d-1663188937091175/

Official website
https://www.kolibri2d.com

Developer
Yeshua Padilla

