### machine translation

# Rimworld automatic machine tool MOD.
- This MOD is made with reference to S.A.L.: Auto-crafters 2.0.(http://steamcommunity.com/sharedfiles/filedetails/?id=932193652)
- This MOD is made with reference to S.A.L.: Extra Crafters.(http://steamcommunity.com/sharedfiles/filedetails/?id=940984361)
- This MOD is made with reference to Industrial Rollers.(http://steamcommunity.com/sharedfiles/filedetails/?id=940984361)  
- This MOD is made with reference to Project RimFactory.(https://steamcommunity.com/sharedfiles/filedetails/?id=1206316724)  
Thanks to the producers of these mods.


---
## AutoMachineTool

![AutoMachineTool](/NR_AutoMachineTool/About/Desc_AutoMachineTool.png)

### Usage
- Research on automatic machine tool.
- Place the AutoMachineTool facing the work table.
- Set the output destination of the finished product.
- The AutomaticMachineTool executes a request that can be worked with materials in eight directions around the work table in the work request.

### Function
- You can specify the output destination. If the output destination is in the stockpiling zone and the finished product can be placed, it is placed somewhere in the stockpiling zone containing the output destination.
- Even if the output destination is in the stockpiling zone, the finished product can not be placed, but it is placed in the cell specified as the output destination.
- You can select the automatic machine tool and change the power supply amount from the power tab. If the power supply amount is large, the creation speed increases.
- As the tier goes up, you can create an AutoMachineTool with a high skill level.
- You can set the upper and lower limits of power supply amount, skill level and speed factors for each tier on the Mod setting screen.

### Caution
- If there is no place to place after completion, processing stops waiting for placement. You can prevent finished goods by moving them on a belt conveyor or the like, or by setting the output destination to a stockpiling zone.
- If you destroy a work table or automatic machine tool during work, material may be lost.
- I think balance is not good.
- If MOD is deleted while there is a processing request for the target work table, save data will be damaged.
- The light color range at installation is the range maximum setting range.

---
## BeltConveyor

![BeltConveyor](/NR_AutoMachineTool/About/Desc_BeltConveyor1.png)  
![Underground conveyor](/NR_AutoMachineTool/About/Desc_BeltConveyor2.png)

### Usage
- Research on belt conveyor.
- Place it in the direction you want to carry.
- Place the item on top. (Item Puller and AutoMachineTool can be used.)
- The belt conveyors are connected by vertically adjoining the orientation.
- The underground conveyor connects to the underground conveyor entrance and exit

### Function
- If belt conveyors are connected and there are two or more output destinations, it is possible to set the filter for each output destination from the filter tab.
- If the output destination can be placed in the stockpiling zone, the output destination is placed somewhere in the stockpiling zone that contains the output destination.
- Even if the destination can not be placed in the stockpiling zone, it will be placed in the cell specified as the output destination.
- There is a belt conveyor that can be placed in the wall.
- You can select the belt conveyor and change the power supply amount from the power tab. If the power supply amount is large, the transfer speed increases.

### Caution
- If there is no place to place it, the item is blocked.
- Colonists are not involved with items on the belt conveyor.
- I think balance is not good.
- With the belt conveyor selected, you can see the underground conveyor.

---
## Item Puller

![ItemPuller](/NR_AutoMachineTool/About/Desc_ItemPuller.png)

### Usage
- Research on belt conveyor.
- Place it next to the pulling zone. Pull out from the opposite side of the installed orientation and output it in the installed orientation.
- Since the drawing machine is in a non-operating state in the post-installation state, it selects the drawing machine and put it in the operating state from the button.

### Function
- You can specify filters for items to be pulled from the filter tab.
- If the output destination can be placed in the stockpiling zone, the output destination is placed somewhere in the stockpiling zone that contains the output destination.
- Even if the destination can not be placed in the stockpiling zone, it will be placed in the cell specified as the output destination.
- There is a Puller that can be placed in the wall.


### Caution
- If the output destination is a belt conveyor and the item already exists, the item will be placed in placement.
- If the output destination is not a belt conveyor and the item already exists, it will be placed near the output destination.
- I think balance is not good.

---
## Planter

![Planter](/NR_AutoMachineTool/About/Desc_Planter.png)

### Usage
- Research on Automatic Agriculture.
- Place the Planter inside or near the agricultural zone.
- Planter starts planting plants in the agricultural zone within range.

### Function
- You can select the Planter and change the power supply amount from the power tab. If the power supply is large, the sowing speed and seeding range will rise.
- As the tier goes up, you can create the Planter with a high skill level.
- You can select the Planter and restrict processing by the stock quantity of the product from the Limitation tab.

### Caution
- Planter will not plant in places that can not be planted by temperature etc.
- Even if there are plants that are not designated as agriculture zones, they will not reap.
- The frame at the time of placement is the frame when electric power is minimum. By setting power after installation the frame expands. It can be avoided by canceling all processing requests or by removing them after disassembling the automatic machine tool.
- I think balance is not good.
- The light color range at installation is the range maximum setting range.

---
## Harvester

![Harvester](/NR_AutoMachineTool/About/Desc_Harvester.png)

### Usage
- Research on Automatic Agriculture.
- Place the Harvester inside or near the agricultural zone.
- Harvester starts harvesting plants in the agricultural zone within range.

### Function
- You can select the Harvester and change the power supply amount from the power tab. If the power supply is large, the harvesting speed and seeding range will rise.
- If the output destination can be placed in the stockpiling zone, the output destination is placed somewhere in the stockpiling zone that contains the output destination.
- Even if the destination can not be placed in the stockpiling zone, it will be placed in the cell specified as the output destination.
- As the tier goes up, you can create the Harvester with a high skill level.
- You can select the Harvester and restrict processing by the stock quantity of the product from the Limitation tab.
- Cut blighted plants.

### Caution
- If the output destination is a belt conveyor and the item already exists, the item will be placed in placement.
- If the output destination is not a belt conveyor and the item already exists, it will be placed near the output destination.
- Harvester will harvest if harvestable, even if there are plants not specified in the agriculture zone.
- The frame at the time of placement is the frame when electric power is minimum. By setting power after installation the frame expands.
- I think balance is not good.
- The light color range at installation is the range maximum setting range.

---
## AnimalResourceGatherer

![AnimalResourceGatherer](/NR_AutoMachineTool/About/Desc_AnimalResourceGatherer.png)

### Usage
- Research on Automatic animal caretaker.
- Place the AnimalResourceGatherer near animals passing through or staying.
- AnimalResourceGatherer starts milking and shearing from animals within range.

### Function
- You can select the AnimalResourceGatherer and change the power supply amount from the power tab. If the power supply is large, the harvesting speed and seeding range will rise.
- If the output destination can be placed in the stockpiling zone, the output destination is placed somewhere in the stockpiling zone that contains the output destination.
- Even if the destination can not be placed in the stockpiling zone, it will be placed in the cell specified as the output destination.
- You can select the AnimalResourceGatherer and restrict processing by the stock quantity of the product from the Limitation tab.

### Caution
- If the output destination is a belt conveyor and the item already exists, the item will be placed in placement.
- If the output destination is not a belt conveyor and the item already exists, it will be placed near the output destination.
- The frame at the time of placement is the frame when electric power is minimum. By setting power after installation the frame expands.
- I think balance is not good.
- The light color range at installation is the range maximum setting range.

---
## Slaughterhouse

![Slaughterhouse](/NR_AutoMachineTool/About/Desc_Slaughterhouse.png)

### Usage
- Research on Automatic animal caretaker.
- Place the Slaughterhouse near animals passing through or staying.
- Select the slaughterhouse and set slaughter from the setting tab.
- Slaughterhouse starts slaughter animals within range.

### Function
- You can select the Slaughterhouse and change the power supply amount from the power tab. If the power supply is large, the harvesting speed and seeding range will rise.
- If the output destination can be placed in the stockpiling zone, the output destination is placed somewhere in the stockpiling zone that contains the output destination.
- Even if the destination can not be placed in the stockpiling zone, it will be placed in the cell specified as the output destination.
- You can select the slaughter machine and set the number of livestock and the availability of slaughter to leave from the setting tab.

### Caution
- If the output destination is a belt conveyor and the item already exists, the item will be placed in placement.
- If the output destination is not a belt conveyor and the item already exists, it will be placed near the output destination.
- The frame at the time of placement is the frame when electric power is minimum. By setting power after installation the frame expands.
- I think balance is not good.
- The light color range at installation is the range maximum setting range.

---
## AutoMiner

![AutoMiner](/NR_AutoMachineTool/About/Desc_Miner.png)

### Usage
- Research on Automatic mining.
- Install in an arbitrary place.
- Select the AutoMiner and set mining from the processing tab.
- The AutoMiner will mining.

### Function
- You can select the AutoMiner and change the power supply amount from the power tab. If the power supply amount is large, the mining speed increases.
- You can specify the output destination. If the output destination is in the stockpiling zone and the finished product can be placed, it is placed somewhere in the stockpiling zone containing the output destination.

### Caution
- If the output destination is a belt conveyor and the item already exists, the item will be placed in placement.
- If the output destination is not a belt conveyor and the item already exists, it will wait for placement.
- I think balance is not good.

---
## AutoCleaner

![AutoCleaner](/NR_AutoMachineTool/About/Desc_Cleaner.png)

### Usage
- Research on Automatic cleaning.
- Install in a location where dirt or dirt will be brought in.
- The AutoCleaner cleans the dirt in the range.

### Function
- You can select the AutoCleaner and change the power supply amount from the power tab. If the power supply is large, the cleaning speed and cleaning range will rise.
- If Pawn within the range is dirty, drop dirt. Then AutoCleaner will clean it.

### Caution
- I think balance is not good.
- The light color range at installation is the range maximum setting range.

---
## WallLight

![WallLight](/NR_AutoMachineTool/About/Desc_WallLight.png)

### Usage
- Install on the wall.

### Function
- It glows.

### Caution

---
## AutoRepairer

![AutoRepairer](/NR_AutoMachineTool/About/Desc_Repairer.png)

### Usage
- Research on Automatic repairing.
- Install it near where you need repair.
- Automatically repair building and pawn's equipment in the range.

### Function
- You can select the AutoRepairer and change the power supply amount from the power tab. If the power supply is large, the speed and range will rise.
- If the HP of the building in the range is decreasing, repair it.
- If the HP of weapons and clothes equipped by Pawn within the range is decreasing, repair it.
- Extinguish the fire within the range.

### Caution
- I think balance is not good.
- The light color range at installation is the range maximum setting range.

---
## Stunner

![Stunner](/NR_AutoMachineTool/About/Desc_Stunner.png)

### Usage
- Research on Automatic stunner.
- Automatically stun hostile pawn in the range.

### Function
- You can select the Stunner and change the power supply amount from the power tab. If the power supply is large, the speed and range will rise.
- Animals in violence will also stun.
- Stunner kills the mechanoid.
- Prisoners are excluded.
- Prisoners under jailbreak are included.
- Fleeing Pawns are excluded.

### Caution
- After a while the stunning condition will be lifted.
- I think balance is not good.
- The light color range at installation is the range maximum setting range.

---
## Material Converter

![material converter](/NR_AutoMachineTool/About/Desc_MaterialMachine.png)

### Usage
- Research on material energy.
- install a material converter.
- Select the scan from the Processing tab and enable the filter for the items you want to scan.
- A recipe to convert scanned items from energy to matter is created.
- Select Energize material from the Processing tab and enable the filter for items to be energized.
- Since energy is generated when it is processed, it saves the amount of energy to convert from energy to matter.
- Select materialization scanned and registered from the Processing tab.
- Energy is used and materialized.

### Function
- The scan creates an energy-to-material conversion recipe.
- Energy conversion from substances is possible in units of 10 and 100.
- Material conversion from energy is possible in units of 1, 10 units.
- Recipes registered by scanning can be deleted.

### Caution
- All scan filters are turned off by default.
- One item is consumed at the time of scanning.
- All energy filters are turned off by default.
- Since conversion loss occurs, even if you return to the same material after energy conversion, it will not be the same number.
- Those with materials such as iron are somewhat required for materials at the time of material conversion.
- I think balance is not good.

---
## Shield Generator

![Shield Generator](/NR_AutoMachineTool/About/Desc_Shield.png)

### Usage
- Research on Shield generator.
- Install Shield generator.
- The shield generator destroys and protects within range, from bullets, hostile faction DropPod, crashed ship part, meteorite.

### Function
- Drop pods other than hostile factions will not be destroyed.
- Player faction bullets will not be destroyed.
- You can select the Shield generator and change the power supply amount from the power tab. If the power supply is large, the range will rise.

### Caution
- I think balance is not good.

---
## PowerfulPowerPlant

![PowerfulPowerPlant](/NR_AutoMachineTool/About/Desc_Generator.png)

### Usage
- Research on Powerful power.
- Install PowerfulPowerPlant.
- Connect the PowerfulPowerPlant and the conduit.

### Function
- generate electricity.

### Caution
- I think balance is not good.

---
## Overall

### TODO
- Debug
- I manage the image. . .
- I want to improve my English. . .
