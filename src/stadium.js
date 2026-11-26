/**
 * One night ground at Lantern Yard. Simple meshes, no shadow maps,
 * so a low-end phone can hold a frame. Gameplay state stays outside.
 */

import {
  AmbientLight,
  BoxGeometry,
  CanvasTexture,
  CircleGeometry,
  Color,
  CylinderGeometry,
  DirectionalLight,
  DoubleSide,
  Fog,
  Group,
  HemisphereLight,
  Mesh,
  MeshBasicMaterial,
  MeshLambertMaterial,
  NoToneMapping,
  PerspectiveCamera,
  PlaneGeometry,
  PointLight,
  RepeatWrapping,
  Scene,
  SphereGeometry,
  SRGBColorSpace,
  TorusGeometry,
  Vector3,
  WebGLRenderer,
} from 'three';

const lookTarget = new Vector3(0, 0.6, 2);

export function createStadium(container, assets) {
  const renderer = new WebGLRenderer({
    antialias: false,
    alpha: false,
    powerPreference: 'high-performance',
    failIfMajorPerformanceCaveat: false,
  });
  renderer.outputColorSpace = SRGBColorSpace;
  renderer.toneMapping = NoToneMapping;
  renderer.setClearColor(0x070b16, 1);
  renderer.domElement.setAttribute('aria-label', 'Lantern Yard at night');
  container.appendChild(renderer.domElement);

  const scene = new Scene();
  scene.background = new Color(0x070b16);
  scene.fog = new Fog(0x070b16, 36, 78);

  const camera = new PerspectiveCamera(52, 1, 0.1, 120);
  camera.position.set(0, 4.6, 15.2);

  scene.add(new AmbientLight(0x9eb4d6, 1.35));
  scene.add(new HemisphereLight(0x1c3158, 0x0c2416, 0.9));
  const moon = new DirectionalLight(0xc5d4ee, 0.55);
  moon.position.set(-8, 14, 6);
  scene.add(moon);

  const grassTex = textureOf(assets.grass);
  grassTex.wrapS = RepeatWrapping;
  grassTex.wrapT = RepeatWrapping;
  grassTex.repeat.set(10, 10);
  const ground = new Mesh(
    new PlaneGeometry(120, 120),
    new MeshLambertMaterial({ map: grassTex }),
  );
  ground.rotation.x = -Math.PI / 2;
  scene.add(ground);

  const pitch = new Mesh(
    new BoxGeometry(3.05, 0.05, 20.12),
    new MeshLambertMaterial({ map: textureOf(assets.pitch) }),
  );
  pitch.position.y = 0.03;
  scene.add(pitch);

  const pool = new Mesh(
    new CircleGeometry(7.5, 24),
    new MeshBasicMaterial({
      map: textureOf(assets.lightPool),
      transparent: true,
      depthWrite: false,
    }),
  );
  pool.rotation.x = -Math.PI / 2;
  pool.position.y = 0.06;
  scene.add(pool);

  const rope = new Mesh(
    new TorusGeometry(28, 0.07, 6, 40),
    new MeshLambertMaterial({ color: 0xf3f4f6 }),
  );
  rope.rotation.x = Math.PI / 2;
  rope.position.y = 0.08;
  scene.add(rope);

  addCreases(scene);
  addNets(scene, assets.net);
  addStands(scene);
  addTowers(scene);
  addSightscreen(scene);
  addSign(scene, assets.sign);

  const nearStumps = makeStumps();
  nearStumps.position.set(0, 0, 9.05);
  scene.add(nearStumps);
  const farStumps = makeStumps();
  farStumps.position.set(0, 0, -9.05);
  scene.add(farStumps);

  const striker = makePlayer({ shirt: 0xe0a045, pad: 0x1d2740, skin: 0xc68642 });
  striker.position.set(0, 0, 8.35);
  const batPivot = new Group();
  batPivot.position.set(0.24, 1.22, -0.02);
  const bat = new Mesh(
    new BoxGeometry(0.18, 0.96, 0.04),
    new MeshLambertMaterial({ color: 0xd7a15a }),
  );
  bat.position.y = -0.38;
  batPivot.add(bat);
  striker.add(batPivot);
  scene.add(striker);

  const partner = makePlayer({ shirt: 0xe0a045, pad: 0x1d2740, skin: 0xa86b45 });
  partner.position.set(1.55, 0, -8.3);
  partner.rotation.y = Math.PI;
  partner.scale.setScalar(0.92);
  scene.add(partner);

  const bowler = makePlayer({ shirt: 0x1f6b4a, pad: 0xe7e1d2, skin: 0x8d552f });
  bowler.position.set(-1.55, 0, -10.6);
  const bowlArm = new Group();
  bowlArm.position.set(0.24, 1.35, 0);
  const arm = new Mesh(
    new BoxGeometry(0.08, 0.42, 0.08),
    new MeshLambertMaterial({ color: 0x8d552f }),
  );
  arm.position.y = -0.16;
  bowlArm.add(arm);
  bowler.add(bowlArm);
  scene.add(bowler);

  const ball = new Mesh(
    new SphereGeometry(0.075, 14, 12),
    new MeshLambertMaterial({ map: textureOf(assets.ball) }),
  );
  scene.add(ball);
  const shadow = new Mesh(
    new CircleGeometry(0.16, 12),
    new MeshBasicMaterial({ color: 0x04140c, transparent: true, opacity: 0.45, depthWrite: false }),
  );
  shadow.rotation.x = -Math.PI / 2;
  shadow.position.y = 0.07;
  scene.add(shadow);

  const marker = new Mesh(
    new CircleGeometry(0.18, 12),
    new MeshBasicMaterial({ color: 0xf0b429, transparent: true, opacity: 0.9, depthWrite: false }),
  );
  marker.rotation.x = -Math.PI / 2;
  marker.position.y = 0.08;
  marker.visible = false;
  scene.add(marker);

  const aim = new Group();
  const aimMesh = new Mesh(
    new BoxGeometry(0.16, 0.02, 1.5),
    new MeshBasicMaterial({ color: 0xf0b429, transparent: true, opacity: 0.75 }),
  );
  aimMesh.position.z = -0.9;
  aim.add(aimMesh);
  aim.position.set(0, 0.09, 8.15);
  scene.add(aim);

  function resize() {
    const width = container.clientWidth;
    const height = container.clientHeight;
    if (width < 2 || height < 2) return;
    const aspect = width / height;
    camera.aspect = aspect;
    camera.fov = aspect < 1.5 ? 60 : 50;
    camera.updateProjectionMatrix();
    renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 1.5));
    renderer.setSize(width, height, false);
  }

  const resizeObserver = new ResizeObserver(() => resize());
  resizeObserver.observe(container);
  resize();

  return {
    resize,
    render() {
      camera.lookAt(lookTarget);
      renderer.render(scene, camera);
    },
    setBall(x, y, z) {
      ball.position.set(x, y, z);
      shadow.position.set(x, 0.07, z);
      const lift = Math.min(1, Math.max(0, y - 0.05));
      shadow.scale.setScalar(1 + lift * 0.8);
      shadow.material.opacity = 0.45 - lift * 0.25;
    },
    setMarker(point) {
      if (!point) {
        marker.visible = false;
        return;
      }
      marker.visible = true;
      marker.position.set(point.x, 0.08, point.z);
    },
    setAim(direction) {
      const lateral = direction?.x ?? 0;
      const straight = direction?.y >= 0 ? 1 : 1 + direction.y;
      const zDir = -(0.45 + 0.55 * straight);
      aim.rotation.y = Math.atan2(lateral, -zDir);
      aim.visible = true;
    },
    setBat(amount) {
      const t = Math.max(0, Math.min(1, amount));
      batPivot.rotation.x = -1.05 + t * 2.05;
      batPivot.rotation.z = -0.15 + t * 0.4;
    },
    setBowler(amount) {
      const t = Math.max(0, Math.min(1, amount));
      bowler.position.z = -10.6 + t * 1.7;
      bowlArm.rotation.x = -t * 2.4;
    },
    dropStumps() {
      nearStumps.rotation.x = 1.15;
      nearStumps.position.y = 0.15;
    },
    resetStumps() {
      nearStumps.rotation.x = 0;
      nearStumps.position.y = 0;
    },
  };
}

function textureOf(source) {
  const tex = new CanvasTexture(source);
  tex.colorSpace = SRGBColorSpace;
  tex.needsUpdate = true;
  return tex;
}

function addCreases(scene) {
  const mat = new MeshBasicMaterial({ color: 0xf7f7f7 });
  const marks = [
    [2.4, 0.02, 8.55],
    [2.4, 0.02, 7.15],
    [2.4, 0.02, -8.55],
    [2.4, 0.02, -7.15],
  ];
  for (const [width, , z] of marks) {
    const crease = new Mesh(new BoxGeometry(width, 0.01, 0.04), mat);
    crease.position.set(0, 0.07, z);
    scene.add(crease);
  }
}

function addNets(scene, source) {
  const tex = textureOf(source);
  tex.wrapS = RepeatWrapping;
  tex.wrapT = RepeatWrapping;
  tex.repeat.set(8, 2);
  const mat = new MeshBasicMaterial({
    map: tex,
    transparent: true,
    opacity: 0.45,
    depthWrite: false,
    side: DoubleSide,
  });
  const side = new PlaneGeometry(20, 2.4);
  const left = new Mesh(side, mat);
  left.position.set(-2.3, 1.2, 0);
  left.rotation.y = Math.PI / 2;
  const right = new Mesh(side, mat);
  right.position.set(2.3, 1.2, 0);
  right.rotation.y = Math.PI / 2;
  const far = new Mesh(new PlaneGeometry(6, 2.6), mat);
  far.position.set(0, 1.3, -10.2);
  scene.add(left, right, far);
}

function addStands(scene) {
  const concrete = new MeshLambertMaterial({ color: 0x243044 });
  const seats = [
    new MeshLambertMaterial({ color: 0x8c3b2a }),
    new MeshLambertMaterial({ color: 0x1e4d6b }),
  ];
  for (const side of [-1, 1]) {
    for (let tier = 0; tier < 3; tier += 1) {
      const block = new Mesh(new BoxGeometry(3.4, 1.05, 26), concrete);
      block.position.set(side * (15.5 + tier * 1.7), 0.55 + tier * 0.85, -1);
      scene.add(block);
      const seat = new Mesh(new BoxGeometry(2.6, 0.16, 24), seats[side > 0 ? 0 : 1]);
      seat.position.set(side * (15.5 + tier * 1.7), 1.12 + tier * 0.85, -1);
      scene.add(seat);
    }
  }
}

function addTowers(scene) {
  const poleMat = new MeshLambertMaterial({ color: 0x8e98a6 });
  const lampMat = new MeshBasicMaterial({ color: 0xfff1c4 });
  for (const [x, z] of [[-20, -12], [20, -12], [-20, 12], [20, 12]]) {
    const pole = new Mesh(new CylinderGeometry(0.12, 0.18, 9, 6), poleMat);
    pole.position.set(x, 4.5, z);
    const head = new Mesh(new BoxGeometry(1.5, 0.28, 0.7), lampMat);
    head.position.set(x, 9.1, z);
    scene.add(pole, head);
    const light = new PointLight(0xffe2b0, 18, 55, 2);
    light.position.set(x, 8.6, z);
    scene.add(light);
  }
}

function addSightscreen(scene) {
  const screen = new Mesh(
    new PlaneGeometry(8, 4.5),
    new MeshLambertMaterial({ color: 0xe6e2d4 }),
  );
  screen.position.set(0, 2.3, -13.2);
  scene.add(screen);
}

function addSign(scene, source) {
  const sign = new Mesh(
    new PlaneGeometry(6.4, 2),
    new MeshBasicMaterial({ map: textureOf(source) }),
  );
  sign.position.set(0, 5.8, -13.15);
  scene.add(sign);
}

function makeStumps() {
  const group = new Group();
  const wood = new MeshLambertMaterial({ color: 0xf3e6c4 });
  for (const x of [-0.11, 0, 0.11]) {
    const stump = new Mesh(new CylinderGeometry(0.025, 0.025, 0.71, 8), wood);
    stump.position.set(x, 0.355, 0);
    group.add(stump);
  }
  const bail = new MeshLambertMaterial({ color: 0xfff8ea });
  for (const x of [-0.055, 0.055]) {
    const piece = new Mesh(new BoxGeometry(0.1, 0.02, 0.02), bail);
    piece.position.set(x, 0.72, 0);
    group.add(piece);
  }
  return group;
}

function makePlayer({ shirt, pad, skin }) {
  const group = new Group();
  const shirtMat = new MeshLambertMaterial({ color: shirt });
  const padMat = new MeshLambertMaterial({ color: pad });
  const skinMat = new MeshLambertMaterial({ color: skin });
  const torso = new Mesh(new BoxGeometry(0.42, 0.52, 0.22), shirtMat);
  torso.position.y = 1.18;
  const head = new Mesh(new SphereGeometry(0.15, 10, 8), skinMat);
  head.position.y = 1.58;
  const helmet = new Mesh(
    new SphereGeometry(0.18, 10, 8),
    new MeshLambertMaterial({ color: 0x1b2330 }),
  );
  helmet.scale.y = 0.82;
  helmet.position.y = 1.62;
  const legGeo = new BoxGeometry(0.12, 0.58, 0.14);
  const legL = new Mesh(legGeo, padMat);
  legL.position.set(-0.1, 0.52, 0);
  const legR = new Mesh(legGeo, padMat);
  legR.position.set(0.1, 0.52, 0);
  group.add(torso, head, helmet, legL, legR);
  return group;
}
