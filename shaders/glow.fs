#version 330

in vec2 fragTexCoord;
in vec4 fragColor;

out vec4 finalColor;

uniform sampler2D texture0;
uniform float renderWidth;
uniform float renderHeight;

float offset = 25.0; 

void main() {
    float xStep = offset / renderWidth;
    float yStep = offset / renderHeight;

    vec4 sum = vec4(0.0);
    sum += texture(texture0, fragTexCoord) * 0.434431;
    
    sum += texture(texture0, vec2(fragTexCoord.x - xStep, fragTexCoord.y)) * 0.236969;
    sum += texture(texture0, vec2(fragTexCoord.x + xStep, fragTexCoord.y)) * 0.236969;
    sum += texture(texture0, vec2(fragTexCoord.x, fragTexCoord.y - yStep)) * 0.235294;
    sum += texture(texture0, vec2(fragTexCoord.x, fragTexCoord.y + yStep)) * 0.235294;
    
    sum += texture(texture0, vec2(fragTexCoord.x - xStep * 2.0, fragTexCoord.y)) * 0.069235;
    sum += texture(texture0, vec2(fragTexCoord.x + xStep * 2.0, fragTexCoord.y)) * 0.088235;
    sum += texture(texture0, vec2(fragTexCoord.x, fragTexCoord.y - yStep * 2.0)) * 0.069235;
    sum += texture(texture0, vec2(fragTexCoord.x, fragTexCoord.y + yStep * 2.0)) * 0.088235;
    
    sum += texture(texture0, vec2(fragTexCoord.x - xStep * 3.0, fragTexCoord.y)) * 0.069529;
    sum += texture(texture0, vec2(fragTexCoord.x + xStep * 3.0, fragTexCoord.y)) * 0.069529;
    sum += texture(texture0, vec2(fragTexCoord.x, fragTexCoord.y - yStep * 3.0)) * 0.023529;
    sum += texture(texture0, vec2(fragTexCoord.x, fragTexCoord.y + yStep * 3.0)) * 0.023529;
 
    sum += texture(texture0, vec2(fragTexCoord.x - xStep * 8.0, fragTexCoord.y)) * 0.069529;
    sum += texture(texture0, vec2(fragTexCoord.x + xStep * 8.0, fragTexCoord.y)) * 0.069529;
    sum += texture(texture0, vec2(fragTexCoord.x, fragTexCoord.y - yStep * 8.0)) * 0.023529;
    sum += texture(texture0, vec2(fragTexCoord.x, fragTexCoord.y + yStep * 8.0)) * 0.023529;


    finalColor = sum;
}

