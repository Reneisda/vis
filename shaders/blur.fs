#version 330

in vec2 fragTexCoord;
in vec4 fragColor;

out vec4 finalColor;

uniform sampler2D texture0;
uniform float renderWidth;
uniform float renderHeight;

float offset = 9.0; 

void main() {
    float xStep = offset / renderWidth;
    float yStep = offset / renderHeight;

    vec4 sum = vec4(0.0);
    sum += texture(texture0, vec2(fragTexCoord.x, fragTexCoord.y)) * 0.2270270270;
    
    sum += texture(texture0, vec2(fragTexCoord.x - xStep, fragTexCoord.y)) * 0.1945945946;
    sum += texture(texture0, vec2(fragTexCoord.x + xStep, fragTexCoord.y)) * 0.1945945946;
    sum += texture(texture0, vec2(fragTexCoord.x, fragTexCoord.y - yStep)) * 0.1945945946;
    sum += texture(texture0, vec2(fragTexCoord.x, fragTexCoord.y + yStep)) * 0.1945945946;
    
    sum += texture(texture0, vec2(fragTexCoord.x - xStep * 2.0, fragTexCoord.y)) * 0.0405405405;
    sum += texture(texture0, vec2(fragTexCoord.x + xStep * 2.0, fragTexCoord.y)) * 0.0405405405;
    sum += texture(texture0, vec2(fragTexCoord.x, fragTexCoord.y - yStep * 2.0)) * 0.0405405405;
    sum += texture(texture0, vec2(fragTexCoord.x, fragTexCoord.y + yStep * 2.0)) * 0.0405405405;

    finalColor = sum;
}

