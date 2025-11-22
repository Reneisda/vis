#version 330

in vec2 fragTexCoord;
in vec4 fragColor;

uniform sampler2D texture0;
uniform vec4 colDiffuse;

uniform float time;
uniform vec2 resolution;

out vec4 finalColor;

float random(vec2 st) {
    return fract(sin(dot(st.xy, vec2(12.9898, 78.233))) * 43758.5453123);
}

void main()
{
    vec2 uv = fragTexCoord;
    
    float glitchStrength = 1.0;  
    float colorSplit = 0.005;    
    float blockCount = 15.0;     
    float noiseSpeed = 10.0;
    float scrollSpeed = 5.0;
    
    float timeStep = floor(time * noiseSpeed);
    float blockY = floor((uv.y * blockCount) + (time * scrollSpeed));
    
    float blockX = floor(uv.x * 2.0); 
    float noise = random(vec2(blockY, blockX + timeStep));
    float displacement = 0.0;
    float threshold = 0.95; // Threshold for glitch activation
    
    if (noise > threshold) {
        float shift = (noise - threshold) * 20.0;
        if (random(vec2(timeStep, blockY)) > 0.5) {
            displacement = shift * 0.05;
        } else {
            displacement = -shift * 0.05;
        }
    }
    
    vec2 rOffset = vec2(colorSplit + displacement, 0.0);
    vec2 gOffset = vec2(displacement, 0.0);
    vec2 bOffset = vec2(-colorSplit + displacement, 0.0);
    
    float r = texture(texture0, uv + rOffset).r;
    float g = texture(texture0, uv + gOffset).g;
    float b = texture(texture0, uv + bOffset).b;
    float a = texture(texture0, uv).a;

    float scanline = sin(uv.y * resolution.y * 3.14159) * 0.05;
    vec3 finalRGB = vec3(r, g, b);
    finalRGB -= scanline;

    finalColor = vec4(finalRGB, a) * colDiffuse;
}

