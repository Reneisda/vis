#version 330

in vec2 fragTexCoord;
in vec4 fragColor;

out vec4 finalColor;

uniform sampler2D texture0;
uniform float renderWidth;
uniform float renderHeight;
uniform float lightBrightness;
uniform int barCount;
uniform float barHeights[128];
uniform float barWidth;
uniform float barSpacing;
uniform float time;

float decay_rate = 0.94;
float glow_boost = 4.0;
float blur_radius_px = 3.0;


void main() {
    vec2 uv = fragTexCoord;
    vec2 texelSize = vec2(1.0 / renderWidth, 1.0 / renderHeight);
    vec4 current_content = texture(texture0, uv);
    vec3 faded_content = current_content.rgb * decay_rate;

    float blurred_mask = 0.0;
    
    blurred_mask += texture(texture0, uv - texelSize * blur_radius_px * 2.0).r * 0.1; // Far Left
    blurred_mask += texture(texture0, uv - texelSize * blur_radius_px * 1.0).r * 0.2; // Mid Left
    blurred_mask += current_content.r * 0.4;                                           // Center
    blurred_mask += texture(texture0, uv + texelSize * blur_radius_px * 1.0).r * 0.2; // Mid Right
    blurred_mask += texture(texture0, uv + texelSize * blur_radius_px * 2.0).r * 0.1; // Far Right
    
    blurred_mask /= (0.1 + 0.2 + 0.4 + 0.2 + 0.1); 

    float x_coord_px = uv.x * renderWidth;
    int index = int(x_coord_px / (barWidth + barSpacing));
    
    float bar_power = 0.0;
    if (index >= 0 && index < barCount) {
        bar_power = barHeights[index];
    }
    
    vec3 pulse_color = mix(
        vec3(0.0, 0.4, 0.8),
        vec3(1.0, 0.2, 0.0),
        bar_power * bar_power 
    );

    vec3 final_rgb = mix(faded_content, pulse_color * glow_boost, blurred_mask);
    finalColor = vec4(final_rgb * lightBrightness, 1.0);
}

