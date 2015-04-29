# Enhanced Hearse AI
Steam Workshop: [[ARIS] Enhanced Hearse AI](http://steamcommunity.com/sharedfiles/filedetails/?id=433249875)  
Requires: [[ARIS] Skylines Overwatch](https://github.com/arislancrescent/CS-SkylinesOverwatch)

Oversees death services to ensure that hearses are dispatched in an efficient manner.

There are two sets of important concepts to keep in mind: 

1. Inside vs outside a district
2. Primary vs secondary pickup zone

## Inside a District
If a cemetery/crematorium is inside a district, then:

1. Its primary pickup zone is the district
2. Its secondary pickup zone is its effective area, which is specified by the game as a radius range with the building as the center. This area is approximately 50% of one tile.

## Outside a District
If a cemetery/crematorium does not belong to any district, then:

1. Its primary pickup zone include all the areas within its effective area that does not belong to a district
2. Its secondary pickup zone is the rest of its effective area

## Pickup Priority
Hearses will always prioritize corpses in their primary pickup zone. However, there are several rules within this general rule:

1. Between problematic corpses (those showing the death sign) and nonproblematic ones (those that have no visual clues), hearses will prioritize the problematic ones for pickup
2. However, if they come across a nonproblematic one along their way, they will pick it up first; but only if it is not behind them
3. If there is a closer corpse of the same priority, they will redirect to the closer one; but only if it is along the original bearing 
4. If there are no corpses left to pick up, hearses will "patrol" their primary pickup zone.

## Efficiency vs Urgency
The pickup priority above exists to achieve a good balance between making the hearses as efficient as possible vs keeping your CIMs as happy as possible. When you see a death sign, that means a corpse has become a nusance and your CIMs are not happy about it. But if we were to prioritize getting rid of the death signs as fast as possible, we would have to take a large hit on overall efficiency; in fact, it would become counterproductive during death waves. On the other hand, if we were to do the opposite, then we would be ensuring maximum efficiency at the risk of losing buildings to abandonment. The existing setup benefits from both approaches by prioritizing problematic corpses for overall bearing, but at the same time allowing pickups of nonproblematic corpses along the way.

## Conflict Resolution
Each cemetery/crematorium gets its own dispatcher. The dispatcher will try to maximize the efficiency of all its hearses, i.e., reduce the chance that two hearses will be sent to the same location for pick up. HOWEVER, just like in real life, the dispatchers of different cemeteries/crematoriums will not call each other constantly to make sure they are not all dispatching for the same corpse. So, if you have multiple cemeteries right next to each other, it is possible that a corpse will be fought over by hearses from different cemeteries. With that said, it shouldn't be a common occurrence.

## Why is my hearse stopping at a house without the death sign?
Because it has a corpse. The game only puts up the death sign if a corpse has been sitting there for an extended period of time. One way to confirm this is to follow a hearse around. When it stops at one of those houses, you will be able to see that its load percentage increases.
