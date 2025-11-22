#version 330

in vec2 fragTexCoord;
out vec4 finalColor;

uniform sampler2D texture0;
uniform float renderWidth;
uniform float renderHeight;
uniform float lightBrightness;

uniform int barCount;
uniform float barHeights[128];
uniform float barWidth;
uniform float barSpacing;

float sample_count = 10.0;
float minRadius = 30.0;
float maxRadius = 500.0;
float minIntensity = 5.5;
float maxIntensity = 40.5;

float get_bar_height(float u_coord, out float barHeight) {
    float x_px = u_coord * renderWidth;
    int index = int(x_px / (barWidth + barSpacing));
    if (index >= 0 && index < barCount) {
        barHeight = barHeights[index];
        return 1.0 + (barHeight * 100.0);
    }
    return 1.f;
}

void main() {
    vec2 uv = fragTexCoord;
    vec2 texelSize = vec2(1.0/renderWidth, 1.0/renderHeight);

    vec4 base = texture(texture0, uv);
    if (base.a < 0.01) {
        finalColor = vec4(0.0);
        return;
    }

    float barHeight;
    get_bar_height(uv.x, barHeight);

    // Scale glow radius and intensity by bar height
    float glow_radius    = mix(minRadius, maxRadius, barHeight);
    float glow_intensity = mix(minIntensity, maxIntensity, barHeight);
    if (glow_radius < 1.0) glow_radius = 1.0;

    // --- 2D Blur ---
    vec3 blur_sum = vec3(0.0);
    float total_weight = 0.0;

    float stride = glow_radius / sample_count;
    float sigma = glow_radius / 2.0;
    float sigma2 = 2.0 * sigma * sigma;

    for (float x=-sample_count; x<=sample_count; x+=1.0) {
        for (float y=-sample_count; y<=sample_count; y+=1.0) {
            vec2 offset = vec2(x * stride * texelSize.x, y * stride * texelSize.y);
            vec2 sample_uv = clamp(uv + offset, 0.0, 1.0);
            float dist = length(offset);
            float weight = exp(-(dist*dist)/sigma2);
            blur_sum += texture(texture0, sample_uv).rgb * weight;
            total_weight += weight;
        }
    }

    vec3 blurred_color = blur_sum / total_weight;

    // Combine with base color
    vec3 glow = blurred_color * glow_intensity;
    float glow_lum = length(glow);
    vec3 white_hot = vec3(1.0) * max(0.0, glow_lum-1.0) * 0.5;

    vec3 final_rgb = base.rgb + glow + white_hot;
    finalColor = vec4(clamp(final_rgb*lightBrightness, 0.0, 1.0), 1.0);
}

