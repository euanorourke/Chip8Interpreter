using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Diagnostics;

using SDL2;
using System.Runtime.InteropServices;

namespace CHIP8
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.SetWindowSize(50, 3);
            Console.WriteLine("Please select a game (e.g games/PONG): ");
            if (SDL.SDL_Init(SDL.SDL_INIT_EVERYTHING) < 0)
            {
                Console.WriteLine("SDL2 failed to initialise!");
                return;
            }

            IntPtr window = SDL.SDL_CreateWindow("Chip8 emulator", 500, 128, 64 * 8, 32 * 8, SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE);

            if (window == IntPtr.Zero)
            {
                Console.WriteLine("Sdl invalid renderer");
            }

            IntPtr renderer = SDL.SDL_CreateRenderer(window, -1, SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED);

            if (renderer == IntPtr.Zero)
            {
                Console.WriteLine("Sdl invalid renderer");
            }

            


            CPU cpu = new CPU();
            //Parse the opcodes
            
            string game = Console.ReadLine();
            string fullPath = "games/" + game;
            using (BinaryReader reader = new BinaryReader(new FileStream(fullPath, FileMode.Open)))
            {
                List<byte> program = new List<byte>();

                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    // set opcode to the 8 byte hex code
                    program.Add(reader.ReadByte());
                    
                    
                    
                }
                cpu.LoadProgram(program.ToArray());
            } 

            SDL.SDL_Event sdlEvent;
            bool running = true;

            IntPtr sdlSurface, sdlTexture = IntPtr.Zero;


            while (running)
            {
              
                
                cpu.Step();
                while (SDL.SDL_PollEvent(out sdlEvent) != 0)
                {
                    if (sdlEvent.type == SDL.SDL_EventType.SDL_QUIT)
                    {
                        running = false;
                    }
                    else if (sdlEvent.type == SDL.SDL_EventType.SDL_KEYDOWN)
                    {
                        var key = KeyCodeToKey((int)sdlEvent.key.keysym.sym);
                        Console.WriteLine(sdlEvent.key.keysym.sym);
                        cpu.Keyboard |= (ushort)key;
                    }
                    else if (sdlEvent.type == SDL.SDL_EventType.SDL_KEYUP)
                    {
                        var key = KeyCodeToKey((int)sdlEvent.key.keysym.sym);
                        cpu.Keyboard &= (ushort)~key;
                    }
                }
                var displayHandle = GCHandle.Alloc(cpu.Display, GCHandleType.Pinned);

                if (sdlTexture != IntPtr.Zero)
                {
                    SDL.SDL_DestroyTexture(sdlTexture);
                }
                

                sdlSurface = SDL.SDL_CreateRGBSurfaceFrom(displayHandle.AddrOfPinnedObject(), 64, 32, 32, 64 * 4, 0x000000ff, 0x0000ff00, 0x00ff0000, 0xff000000);
                sdlTexture = SDL.SDL_CreateTextureFromSurface(renderer, sdlSurface);

                displayHandle.Free();


                SDL.SDL_RenderClear(renderer);
                SDL.SDL_RenderCopy(renderer, sdlTexture, IntPtr.Zero, IntPtr.Zero);
                SDL.SDL_RenderPresent(renderer);
                Thread.Sleep(1);
                
                
            }
            
        }

           

        private static int KeyCodeToKey(int keycode) // converts ascii to keyindex
        {
            int keyIndex = 0;
            if (keycode < 58)
            {
                keyIndex = keycode - 48;
            }
            else keyIndex = keycode - 87;
            return (1 << keyIndex);
        }

    }

   



    public class CPU
    {
        public byte[] RAM = new byte[4096]; //4096b for 4k of memory
        public byte[] V = new byte[16];
        public ushort I = 0;
        public ushort PC = 0;
        public Stack<ushort> Stack = new Stack<ushort>();
        public byte DelayTimer; //both clocks run at 60hz
        public byte SoundTimer; //both clocks run at 60hz
        public ushort Keyboard;

        public uint[] Display = new uint[64 * 32];

       
        
        private Random rand = new Random(Environment.TickCount);

        public bool WaitingForKeyPress = false;

        private void InitializeFont()
        {
            byte[] characters = new byte[] { 0xF0, 0x90, 0x90, 0x90, 0xF0, 0x20, 0x60, 0x20, 0x20, 0x70, 0xF0, 0x10, 0xF0, 0x80, 0xF0, 0xF0, 0x10, 0xF0, 0x10, 0xF0, 0x90, 0x90, 0xF0, 0x10, 0x10, 0xF0, 0x80, 0xF0, 0x10, 0xF0, 0xF0, 0x80, 0xF0, 0x90, 0xF0, 0xF0, 0x10, 0x20, 0x40, 0x40, 0xF0, 0x90, 0xF0, 0x90, 0xF0, 0xF0, 0x90, 0xF0, 0x10, 0xF0, 0xF0, 0x90, 0xF0, 0x90, 0x90, 0xE0, 0x90, 0xE0, 0x90, 0xE0, 0xF0, 0x80, 0x80, 0x80, 0xF0, 0xE0, 0x90, 0x90, 0x90, 0xE0, 0xF0, 0x80, 0xF0, 0x80, 0xF0, 0xF0, 0x80, 0xF0, 0x80, 0x80 };
            Array.Copy(characters, RAM, characters.Length);
        }

        public void LoadProgram(byte[] program)
        {
            RAM = new byte[4096];
            InitializeFont();
            for (int i = 0; i < program.Length; i++)
            {
                RAM[512 + i] = program[i];
              
            }
            PC = 512;
        }

        private Stopwatch watch = new Stopwatch();


        public void Step() //Execution of opcodes that have been decoded from hex
        {
            if (!watch.IsRunning) watch.Start();
            if (watch.ElapsedMilliseconds > 16)
            {
                if (DelayTimer > 0) DelayTimer--;
                if (SoundTimer > 0) SoundTimer--;
                watch.Restart();
            }
            var opcode = (ushort)((RAM[PC] << 8) | RAM[PC + 1]); 

            if (WaitingForKeyPress)
            {
               
                throw new Exception("NOT SUPPORTED :(");
                return;
            }
           
            ushort nibble = (ushort)(opcode & 0xF000);

            PC += 2;

            switch (nibble)
            {
                case 0x0000: // If the first letter of the opcode is 0 (00E0, 00EE) 
                    if (opcode == 0x00e0)
                    {
                        for (int i = 0; i < Display.Length; i++) Display[i] = 0; //Clear display, 00E0
                    }
                    else if (opcode == 0x00ee)
                    {
                        PC = Stack.Pop();    //Return subroutine, 00EE
                    }
                    else
                    {
                        throw new Exception($"UNKNOWN OPCODE {opcode.ToString("X4")}"); //If opcode is unknown (probably 0NNN) an exception will be thrown
                    }
                    break;


                case 0x1000: // If the first letter of the opcode is 1 (1NNN) 
                    PC = (ushort)(opcode & 0x0FFF);
                    break;

                case 0x2000: // If the first letter of the opcode is 2 (2NNN) 
                    Stack.Push(PC);
                    PC = (ushort)(opcode & 0x0FFF);
                    break;

                case 0x3000: // If the first letter of the opcode is 3 (3XNN) 
                    if (V[(opcode & 0x0F00) >> 8] == (opcode & 0x00FF)) PC += 2;
                    break;

                case 0x4000: // If the first letter of the opcode is 4 (4XNN) 
                    if (V[(opcode & 0x0F00) >> 8] != (opcode & 0x00FF)) PC += 2;
                    break;

                case 0x5000: // If the first letter of the opcode is 5 (4XNN) 
                    if (V[(opcode & 0x0F00) >> 8] == V[(opcode & 0x00F0) >> 4]) PC += 2;
                    break;

                case 0x6000:
                    V[(opcode & 0x0F00) >> 8] = (byte)(opcode & 0x00FF);
                    break;

                case 0x7000:
                    V[(opcode & 0x0F00) >> 8] += (byte)(opcode & 0x00FF);
                    break;

                case 0x8000:    // If the first letter of the opcode is 8 (ALL ARITHMATIC)
                    int vx = (opcode & 0x0F00) >> 8;
                    int vy = (opcode & 0x00F0) >> 4;

                    switch (opcode & 0x000F)
                    {
                        case 0: V[vx] = V[vy]; break; 


                        case 1: V[vx] = (byte)(V[vx] | V[vy]); break;

                        case 2: V[vx] = (byte)(V[vx] & V[vy]); break;

                        case 3: V[vx] = (byte)(V[vx] ^ V[vy]); break;

                        case 4:
                            V[15] = (byte)(V[vx] + V[vy] > 255 ? 1 : 0);
                            V[vx] = (byte)((V[vx] + V[vy]) & 0x00FF);
                            break;

                        case 5:
                            V[15] = (byte)(V[vx] > V[vy] ? 1 : 0);
                            V[vx] = (byte)((V[vx] - V[vy]) & 0x00FF);
                            break;

                        case 6:
                            V[15] = (byte)(V[vx] & 0x0001);
                            V[vx] = (byte)(V[vx] >> 1);
                            break;

                        case 7:
                            V[15] = (byte)(V[vy] > V[vx] ? 1 : 0);
                            V[vx] = (byte)(V[vy] - V[vx] & 0x00FF);
                            break;

                        case 14:
                            V[15] = (byte)(((V[vx] & 0x80) == 0x80) ? 1 : 0);
                            V[vx] = (byte)(V[vx] << 1);
                            break;
                        default:
                            throw new Exception($"UNKNOWN OPCODE {opcode.ToString("X4")}");

                    }
                    break;

                case 0x9000:
                    if (V[(opcode & 0x0F00) >> 8] != V[(opcode & 0x00F0) >> 4]) PC += 2;
                    break;

                case 0xA000:
                    I = (ushort)(opcode & 0x0FFF);
                    break;

                case 0xB000:
                    PC = (ushort)((opcode & 0x0FFF) + V[0]);
                    break;

                case 0xC000:
                    V[(opcode & 0x0F00) >> 8] = (byte)(rand.Next() & (opcode & 0x00FF));
                    break;

                case 0xD000:
                    int x = V[(opcode & 0x0F00) >> 8];
                    int y = V[(opcode & 0x00F0) >> 4];
                    int n = opcode & 0x000F;

                    V[15] = 0;
                    bool displayDirty = false;

                    for (int i = 0; i < n; i++)
                    {
                        byte mem = RAM[I + i];

                        for (int j = 0; j < 8; j++)
                        {
                            byte pixel = (byte)((mem >> (7 - j)) & 0x01);
                            int index = x + j + (y + i) * 64;

                            if (index > 2047) continue;

                            if (pixel == 1 && Display[index] != 0) V[15] = 1;

                            Display[index] = (Display[index] != 0 && pixel == 0) || (Display[index] == 0 && pixel == 1) ? 0xffffffff : 0;//(byte)(Display[index] ^ pixel);
                        }
                    }

                    if (displayDirty)
                    {
                       Thread.Sleep(20);
                    }

                   // DrawDisplay();
                    break;

                case 0xE000:
                    if ((opcode & 0x00FF) == 0x009E)
                    {
                        if (((Keyboard >> V[(opcode & 0x0F00) >> 8]) & 0x01) == 0x01) PC += 2;
                        break;
                    }
                    else if ((opcode & 0x00FF) == 0x00A1)
                    {
                        if (((Keyboard >> V[(opcode & 0x0F00) >> 8]) & 0x01) != 0x01) PC += 2;
                        break;
                    }
                    else throw new Exception($"UNKNOWN OPCODE {opcode.ToString("X4")}");
                case 0xF000:
                    int tx = (opcode & 0x0F00) >> 8;
                    

                    switch (opcode & 0x00FF)
                    {
                        case 0x07:
                            V[tx] = DelayTimer;
                            break;
                        case 0x0A:
                            WaitingForKeyPress = true;
                            PC -= 2;
                            return;
                            
                        case 0x15:
                            DelayTimer = V[tx];
                            break;
                        case 0x1E:
                            I = (ushort)(I + V[tx]);
                            break;
                        case 0x29:
                            I = (ushort)(V[tx] * 5);
                            break;
                        case 0x33:
                            RAM[I] = (byte)(V[tx] / 100);
                            RAM[I + 1] = (byte)((V[tx] % 100) / 10);
                            RAM[I + 2] = (byte)(V[tx] % 10);
                            break;
                        case 0x55:
                            for (int i = 0; i <= tx; i++)
                            {
                                RAM[I + i] = V[i];
                            }
                            break;
                        case 0x65:
                            for (int i = 0; i <= tx; i++)
                            {
                                V[i] = RAM[I + i];
                            }
                            break;
                           
                    }
                    
                     
                    break;


                    

                default:
                    throw new Exception($"UNKNOWN OPCODE {opcode.ToString("X4")}");
                   
            }
            
        }

        
        
    }
}
