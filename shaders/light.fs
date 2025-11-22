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
float glow_boost = 6.0;
float blur_radius_px = 5.0;

// wizard do the shading magic
void main() {
    vec2 uv = fragTexCoord;
    vec2 texelSize = vec2(1.0 / renderWidth, 1.0 / renderHeight);

    vec4 current_content = texture(texture0, uv);
    vec3 faded_content = current_content.rgb * decay_rate;

    float blurred_mask = 0.0;
    blurred_mask += texture(texture0, uv - texelSize * blur_radius_px * 2.0).r * 0.1; 
    blurred_mask += texture(texture0, uv - texelSize * blur_radius_px * 1.0).r * 0.2; 
    blurred_mask += current_content.r * 0.4;                                           
    blurred_mask += texture(texture0, uv + texelSize * blur_radius_px * 1.0).r * 0.2; 
    blurred_mask += texture(texture0, uv + texelSize * blur_radius_px * 2.0).r * 0.1; 
    // woooooshhh
    blurred_mask /= (0.1 + 0.2 + 0.4 + 0.2 + 0.1); 

    float x_coord_px = uv.x * renderWidth;
    int index = int(x_coord_px / (barWidth + barSpacing));
    
    float bar_power = 0.0;
    if (index >= 0 && index < barCount) {
        bar_power = barHeights[index];
    }
    
    vec3 glow_color = mix(vec3(0.0, 0.4, 0.8), vec3(1.0, 0.2, 0.0), bar_power * bar_power);
    vec3 final_rgb = faded_content;
    final_rgb += glow_color * glow_boost * blurred_mask;

    final_rgb += current_content.rgb;
    finalColor = vec4(final_rgb * lightBrightness, 1.0);
}

