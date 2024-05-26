shader_type particles;

uniform sampler2D noise;
uniform float noiseScale = 320;
uniform vec3 colorA : source_color = vec3(1.0, 1.0, 1.0);
uniform vec3 colorB : source_color = vec3(1.0, 1.0, 1.0);

float rand_from_seed(in uint seed) {
  int k;
  int s = int(seed);
  if (s == 0)
    s = 305420679;
  k = s / 127773;
  s = 16807 * (s - k * 127773) - 2836 * k;
  if (s < 0)
    s += 2147483647;
  seed = uint(s);
  return float(seed % uint(65536)) / 65535.0;
}

uint hash(uint x) {
  x = ((x >> uint(16)) ^ x) * uint(73244475);
  x = ((x >> uint(16)) ^ x) * uint(73244475);
  x = (x >> uint(16)) ^ x;
  return x;
}

void start() {
    uint alt_seed1 = hash(NUMBER + uint(1) + RANDOM_SEED);
    uint alt_seed2 = hash(NUMBER + uint(27) + RANDOM_SEED);
    uint alt_seed3 = hash(NUMBER + uint(43) + RANDOM_SEED);
    uint alt_seed4 = hash(NUMBER + uint(111) + RANDOM_SEED);
    uint alt_seed5 = hash(NUMBER + uint(420) + RANDOM_SEED);
    
    CUSTOM.x = rand_from_seed(alt_seed1);
    vec3 position = vec3(rand_from_seed(alt_seed2) * 800.0 - 400.0,
                        0,
                        rand_from_seed(alt_seed4) * 400.0 - 200.0);
                       
    vec3 emitterPlanarPos = EMISSION_TRANSFORM[3].xyz;
    emitterPlanarPos.y = 1.0; 
    TRANSFORM[3].xyz = emitterPlanarPos + position * 0.5;
    CUSTOM.w = 1.0;
    CUSTOM.z = rand_from_seed(alt_seed3);
    COLOR.rgb = mix(colorA, colorB, rand_from_seed(alt_seed5));
    
}

void process() {
    if (RESTART) {
        COLOR.a = 0.7;
    }
    CUSTOM.y += DELTA / LIFETIME;
    CUSTOM.y = mix(CUSTOM.y, 1.0, INTERPOLATE_TO_END);

    // Extract position from transform matrix
    vec3 worldposition = TRANSFORM[3].xyz;
    
    float sampleO = texture(noise, worldposition.xz / noiseScale).r;
    float sampleY = texture(noise, (worldposition.xz+vec2(0.0, 1.0)) / noiseScale).r;
    float sampleX = texture(noise, (worldposition.xz+vec2(1.0, 0.0))  / noiseScale).r;
    float gradientX = (sampleO - sampleX);
    float gradientY = (sampleO - sampleY);
    
    
    // Sample noise textures for x and y velocity components
    vec2 gradient = vec2(-gradientY, gradientX);
    //= textureGrad(noise, worldposition.xz / noiseScale, vec2(0,0), vec2(0,0));
    if (length(gradient) < 0.0045) {
        ACTIVE = false;
    }
    
    vec3 facingDirection = normalize(vec3(gradient.x, 0.0, gradient.y));
    TRANSFORM[0] = vec4(cross(facingDirection, vec3(0.0, 1.0, 0.0)), 0.0);
    TRANSFORM[1] = vec4(0.0, 1.0, 0.0, 0.0);
    TRANSFORM[2] = vec4(facingDirection, 0.0);
    
    COLOR.a = clamp(COLOR.a - DELTA/LIFETIME, 0.1, 1.0);
    
    float scale = CUSTOM.z * clamp(length(gradient) * 10.0, 0.2, 1.0);
    TRANSFORM = TRANSFORM * mat4(
        vec4(scale, 0.0, 0.0, 0.0),
        vec4(0.0, scale, 0.0, 0.0),
        vec4(0.0, 0.0, scale, 0.0),
        vec4(0.0,0.0,0.0,1.0)
    );

    // Combine noise values with a desired scale factor
    VELOCITY = vec3(gradient.x, 0.0, gradient.y) * 180.0 * (2.0 - CUSTOM.z);
    

}