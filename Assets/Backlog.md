# Firewall
## Backlog

### Ideas
  * Packets usualy come in bunches and intensity is wavy
* Questionable
  * Progressive acceleration/deceleration
  * Change constants so that packet speed should balance line quantity
    * Intensity is varriable
    * Speed = k/line\_q
      * k is hardset varriable
    * Questions
      * What if lines are full? what to do to intensity? Or, maybe, we sould still do smth to k? Or should we allow user to adjust k manually?
  * sandbox mode after victory
  * If player drops good packet, it goes again
    * Why? On small number of words it makes little sense
  * Ability to drop all the packets on buffer with superblow that influences intensity, level or score
### Featuring
* Display victory message
* Modularity
  * Dispatch some load from croupier
    * Audio play to external events
    * Difficulty regulation to a new class
    * Extract challenge time to level governing class
* Create interfacing class for minigame.
* Typo indication
  * Counting problem keys
* On-the-run localization
  * Using streaming assets
### Bugz
None so far
### Troubles
* Mechanic lacks means to encourage accuracy
## Learning principles
1. Accuracy first
1. Frequent repetition
1. Continuous enhardening
1. Immediate feedback
## Results & experience
* Main mechanic is playable
* Though it has a few troubles
* And advantages as well
  * encourages blind typing
  * supports multitasking development
* I've had hard time planning responsibilities and divide them to behavior classes

## Perspective
Publish demo using WebGL.

Check Android input capabilities. Normalize difficulty with respect to line quantity and create demo for mobile.

Define minigame interface. Export minigame functionality.