CC = gcc
CFLAGS = -Wall -O3 -Wextra
LIBS = -lraylib -lm -lpulse -lkissfft-float


vis: visualizer.o vis_data_provider.o
	$(CC) $(CFLAGS) $(LIBS) visualizer.o vis_data_provider.o -o vis

visualizer.o: visualizer.c
	$(CC) $(CFLAGS) $(LIBS) -c visualizer.c

vis_data_provider.o: vis_data_provider.c
	$(CC) $(CFLAGS) $(LIBS) -c vis_data_provider.c

clean:
	rm -f *.o vis

clean_obs:
	rm -f *.o

all: vis clean_obs
