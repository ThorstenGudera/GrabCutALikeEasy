GrabCut revisited: Just grab it, dont cut it.

The well known GrabCut algorithm is known to be reliable for segmenting images, but needs a lot of resources, both Memory and CPU-time.
And: when using a MicCut algorithm being written from scratch and not coming from a specialized library, it may turn out, that the time of running the process will be [very] long.
So, how to speed things up and lower the resources needed, by getting almost the same results?

The answer is simple: You dont need a MinCut and so dont need the whole Graph and the ResidualGraph, and, you also dont need to compute the BetaSmootheness function.
This means: You can tweak the results you get from the GMMs for the terminal node capacities of the unknown pixels to get a ready to use partition of the image.
We add, multiply, shift, typecast and, the most significant method, threshold the values.

So start by defining a rectangle containing the portion or the image to segment out.
Classify the pixels in Background, Foreground, probably Background and probably Foreground states by setting up a mask.
Init the GaussinaMixtureModels, one for the Background, one for the Foreground.
Get the Probabilities from the GMMs for the unkonwn pixels as penalties (crossed over: Background GMM for the Foreground pixels and vice versa, as usual).
Take the negative Log as usual to get a set of Capacities for the terminal links of the unknown area.
Now threshold these results (and do other computations on it, if needed).
Get the Capacities for the known pixels.
Use the data we now have to partition the image.

Thats all.

A lot of example code was used writing this demo app, so e.g.:

grabcutmaster: https://github.com/moetayuko/GrabCut/tree/master

bayesian matting (plus closedformmatting): https://github.com/MarcoForte/bayesian-matting and https://github.com/MarcoForte/closed-form-matting

geeksforgeeks push rel: https://www.geeksforgeeks.org/fifo-push-relabel-algorithm/

paper für bk: https://discovery.ucl.ac.uk/id/eprint/13383/1/13383.pdf

I maybe havent implemented the BoykovKolmogorov Algorithm correctly. I did it from the PseudoCode in the paper, but mine sometimes doesnt stop. 
The queue doesnt get empty, so I aded a Check-Method that compares the current path to the last checked path and breaks when similarity exceeds a given amount.

Note: Writes cached files to: LocalApplicationData\Thorsten_Gudera\...

Usage:
- Start the app (WinForms, fw 4.8) and open an Image, click the go-button to open the GrabCut form.
- Draw a rectangle with the mouse, set the parameters like the threshold-value and click onto the Go-button. Sometimes its good to lower the number of components to return, try a value of 1.
[Optional]
- On the right pane, check the draw on result checkbox and draw with the mouse onto regions that should be the type you selected from the combobox (eg. Background, Foregound etc)
- Click onto the Go-button again

- You can optionally do some processing on the outline of the result, simple feathering, or bayesian matting.

![Bild1_354](https://github.com/user-attachments/assets/96aef6a5-bc27-479a-9485-9beb4c76b5ff)

