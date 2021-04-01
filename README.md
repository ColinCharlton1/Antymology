# Assignment 3: Antymology

This project implements an evolutionary algorithm where the genes of each ant are an array which represents the Neural Network used by the ant to make decisions. Similar to the example given in the Assignment outline: https://youtu.be/zIkBYwdkuTk. One interesting aspect of the Neural Network Architecture I implemented is that the input to the ants includes a value which represents the collective feedback of one of the outputs of all of the ants. It also includes an input to the ants which is an output fron the Queen. The hope is that they could have some very minor form of communication through this. I apologise for any difficulty understanding the layout of the neural networks in the code, I chose to represent them as a one dimensional array to make the genetic functions simple and also just becasue I wanted to try it. As a result, the forward pass function is kinda messy as I just used some for loops and saved indexes. The Networks use the leaky ReLU activation function as described here https://towardsdatascience.com/activation-functions-neural-networks-1cbd9f8d91d6.

The algorithm starts by running through an initialization phase which is meant to filter out randomised genes which have a very limited amount of outputs and either only choose one action repeatedly, or get stuck in cycles. 

For Example:

![RaveAnts](https://user-images.githubusercontent.com/47436644/113242821-19fd7c00-926f-11eb-8206-060924ac9a90.gif)

The UI as shown above is very simple, just 4 text outputs representing different measures. The output with the most importance is the NestBlocks value. This output represents the number of nest blocks the Queen of the colony has created. This is the main measure of a Colony's success.

After finishing the initialization phase, the algorithm repeats the Calculate Fitness, Select Individuals, Mutate Individuals, cycle.
In this cycle, a colony is ran from ants created using genetics in a waiting queue of created individuals. To create the waiting que, after each generation a selection is ran using the Tournament Selection method as described here https://www.tutorialspoint.com/genetic_algorithms/genetic_algorithms_parent_selection.htm.

The Mutations used are:
- random mutation of a random number and selection of genes in a selected genetic code
- random mutation of a very small random number and selection of genes in a selected genetic code
- Recombination of a completely random unconnected selection of genes from two selected parents genetic codes.
- Recombination of randomly selected connected sections representing 1/10th of the genes from two selected parents.

The algorithm will run forever, no stopping point was put in.

The genetics and functions of the QueenAnt and the normal Ants are very similar in many ways. I made the choice to seperate them and gave myself many headaches due to that. In hindsight, I may have combined the genetics of the Queen and Ants, as the Queens have some trouble evolving sometimes due to the low amount of evaluations they get (1 compared to 100).

Here's the furthest point I let them Evolve to: 

* Will add later once I let them run overnight *

The code and resources from the main branch that was provided with the assignment were used with very light modification, and many code design and layout choices were made based on the provided code. 
The ant models were created using MagickaVoxel https://ephtracy.github.io/index.html?page=mv_main.
