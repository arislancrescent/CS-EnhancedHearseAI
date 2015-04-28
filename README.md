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
Hearses will always prioritize corpses in their primary pickup zone. If there are no corpses left to pick up in both primary and secondary pickup zones, hearses will "patrol" their primary pickup zone.

## Conflict Resolution
Each cemetery/crematorium gets its own dispatcher. The dispatcher will try to maximize the efficiency of all its hearses, i.e., reduce the chance that two hearses will be sent to the same location for pick up. HOWEVER, just like in real life, the dispatchers of different cemeteries/crematoriums will not call each other constantly to make sure they are not all dispatching for the same corpse. So, if you have multiple cemeteries right next to each other, it is possible that a corpse will be fought over by hearses from different cemeteries. With that said, it shouldn't be a common occurrence.

## Why is my hearse stopping at a house without the death sign?
Because it has a corpse. The game only puts up the death sign if a corpse has been sitting there for an extended period of time. One way to confirm this is to follow a hearse around. When it stops at one of those houses, you will be able to see that its load percentage increases.
