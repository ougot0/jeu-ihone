"""
Génère les fichiers de données INITIAUX du jeu (armes, accessoires, revêtements, économie),
y compris la géométrie des modèles 3D provisoires (primitives).

Usage : python Tools/generate_placeholder_data.py Game/Assets/_Project/Resources/Data

ATTENTION : écrase les JSON existants. Une fois l'équilibrage fait à la main dans les JSON,
ne relancez ce script que si vous reportez vos modifications ici.
Conventions : +Z = bouche du canon, +Y = haut, unités en mètres ; cylindre Unity = 2 unités de haut (scale.y = demi-longueur).
"""
import json, sys, os
OUT = sys.argv[1]
def v(x,y,z): return {"x":round(x,4),"y":round(y,4),"z":round(z,4)}
def box(name, mat, pos, scl, rot=(0,0,0)):
    return {"name":name,"shape":"box","material":mat,"position":v(*pos),"rotation":v(*rot),"scale":v(*scl)}
def cz(name, mat, pos, d, length):  # cylindre le long de Z
    return {"name":name,"shape":"cylinder","material":mat,"position":v(*pos),"rotation":v(90,0,0),"scale":v(d,length/2,d)}
def cx(name, mat, pos, d, length):
    return {"name":name,"shape":"cylinder","material":mat,"position":v(*pos),"rotation":v(0,0,90),"scale":v(d,length/2,d)}
def cy(name, mat, pos, d, length, rot=(0,0,0)):
    return {"name":name,"shape":"cylinder","material":mat,"position":v(*pos),"rotation":v(*rot),"scale":v(d,length/2,d)}
def sph(name, mat, pos, d):
    return {"name":name,"shape":"sphere","material":mat,"position":v(*pos),"rotation":v(0,0,0),"scale":v(d,d,d)}
def slot(id, types, mount, pos, rot=(0,0,0), default="", required=False):
    return {"id":id,"types":types,"mount":mount,"defaultAttachment":default,"required":required,"position":v(*pos),"rotation":v(*rot)}
def stats(dmg,acc,rec,rpm,mag,hand,rel):
    return {"damage":dmg,"accuracy":acc,"recoil":rec,"fireRate":rpm,"magazineSize":mag,"handling":hand,"reloadTime":rel}
def trigger_group(z, y=-0.012, guard_len=0.05):
    return [box("trigger_guard","polymer",(0,y-0.012,z),(0.008,0.005,guard_len)),
            box("trigger_guard_front","polymer",(0,y-0.002,z+guard_len/2),(0.008,0.024,0.005)),
            box("trigger","metal",(0,y-0.002,z-0.004),(0.005,0.018,0.006),(-10,0,0))]

weapons = []
# ---------- Pistolet ----------
p = [box("slide","body",(0,0.035,0.02),(0.028,0.032,0.19)),
     box("frame","polymer",(0,0.008,0.015),(0.026,0.022,0.17)),
     cz("barrel_tip","metal",(0,0.04,0.115),0.012,0.012),
     box("grip","polymer",(0,-0.045,-0.055),(0.028,0.1,0.042),(15,0,0)),
     box("grip_texture_l","rubber",(-0.0145,-0.045,-0.055),(0.002,0.07,0.032),(15,0,0)),
     box("grip_texture_r","rubber",(0.0145,-0.045,-0.055),(0.002,0.07,0.032),(15,0,0)),
     box("rear_sight","metal",(0,0.055,-0.065),(0.02,0.008,0.006)),
     box("front_sight","metal",(0,0.055,0.1),(0.004,0.008,0.005)),
     box("ejection_port","metal",(0.0142,0.04,0.035),(0.002,0.013,0.03)),
     box("slide_stop","metal",(-0.0138,0.016,0.0),(0.002,0.005,0.025)),
     box("beavertail","polymer",(0,0.005,-0.078),(0.024,0.01,0.02))]
p += [box("serration_%d"%i,"metal",(0,0.035,-0.06+i*0.008),(0.0285,0.026,0.003)) for i in range(4)]
p += trigger_group(0.02)
weapons.append({"id":"vk9_sentinel","displayName":"VK-9 Sentinel","family":"pistol","fireMode":"semi","pellets":1,
 "description":"Pistolet de service fictif, fiable et maniable. L'arme de départ de tout tireur.",
 "price":0,"unlockLevel":1,"modelId":"","baseStats":stats(28,55,30,400,15,85,1.6),
 "slots":[slot("optic",["optic"],"plaque-p",(0,0.051,-0.03)),
          slot("muzzle",["muzzle"],"filetage-s",(0,0.04,0.121)),
          slot("rail",["light","laser"],"rail-c",(0,-0.003,0.07),(0,0,180)),
          slot("magazine",["magazine"],"mag-p9",(0,-0.045,-0.055),(15,0,0),"mag_p9_std",True),
          slot("cosmetic",["cosmetic"],"",(0,-0.1,-0.07))],
 "parts":p})
# ---------- Revolver ----------
r = [box("frame","body",(0,0.02,0),(0.03,0.05,0.09)),
     cz("cylinder","metal",(0,0.026,0.012),0.044,0.046),
     box("top_strap","body",(0,0.051,0.03),(0.016,0.008,0.1)),
     cz("barrel","metal",(0,0.038,0.1),0.017,0.13),
     box("barrel_rib","body",(0,0.049,0.1),(0.008,0.006,0.13)),
     box("ejector_shroud","body",(0,0.02,0.1),(0.015,0.015,0.11)),
     box("front_sight","metal",(0,0.058,0.158),(0.004,0.012,0.01)),
     box("rear_sight","metal",(0,0.057,-0.02),(0.012,0.006,0.008)),
     box("hammer","metal",(0,0.052,-0.05),(0.008,0.022,0.014),(-30,0,0)),
     box("grip_frame","body",(0,-0.012,-0.045),(0.022,0.04,0.03),(20,0,0))]
r += trigger_group(0.0,-0.008,0.045)
weapons.append({"id":"hollis_warden","displayName":"Hollis R6 Warden","family":"revolver","fireMode":"semi","pellets":1,
 "description":"Revolver à six coups au caractère affirmé. Puissant et précis, mais lent à recharger.",
 "price":1800,"unlockLevel":2,"modelId":"","baseStats":stats(55,68,60,150,6,70,2.8),
 "slots":[slot("optic",["optic"],"plaque-r",(0,0.055,0.05)),
          slot("grip",["grip"],"crosse-r",(0,-0.05,-0.062),(20,0,0),"grip_rev_wood",True),
          slot("cosmetic",["cosmetic"],"",(0,-0.1,-0.08))],
 "parts":r})
# ---------- PM ----------
s = [box("receiver","body",(0,0.03,0.03),(0.05,0.07,0.3)),
     cz("shroud","body",(0,0.035,0.25),0.036,0.14),
     cz("barrel","metal",(0,0.035,0.33),0.016,0.04),
     box("top_rail","metal",(0,0.07,0.03),(0.022,0.01,0.26)),
     box("rear_sight","metal",(0,0.083,-0.08),(0.02,0.016,0.01)),
     box("front_sight","metal",(0,0.08,0.13),(0.006,0.012,0.008)),
     box("grip","polymer",(0,-0.06,-0.045),(0.032,0.095,0.042),(15,0,0)),
     box("magwell","body",(0,-0.01,0.09),(0.036,0.03,0.05)),
     box("charging_handle","metal",(-0.028,0.05,0.12),(0.012,0.01,0.02)),
     box("ejection_port","metal",(0.0255,0.04,0.05),(0.002,0.02,0.05)),
     box("vent_l","metal",(-0.019,0.035,0.25),(0.002,0.01,0.09)),
     box("vent_r","metal",(0.019,0.035,0.25),(0.002,0.01,0.09))]
s += trigger_group(0.01,-0.004)
weapons.append({"id":"kestrel_k45","displayName":"Kestrel K-45","family":"smg","fireMode":"auto","pellets":1,
 "description":"Pistolet-mitrailleur compact à cadence élevée. Idéal en espace confiné.",
 "price":3200,"unlockLevel":3,"modelId":"","baseStats":stats(22,45,25,850,30,80,2.1),
 "slots":[slot("optic",["optic"],"rail-t",(0,0.075,0.0)),
          slot("muzzle",["muzzle"],"filetage-s",(0,0.035,0.35)),
          slot("underbarrel",["grip"],"rail-t",(0,0.017,0.24),(0,0,0)),
          slot("rail",["light","laser"],"rail-t",(0.025,0.03,0.12),(0,0,-90)),
          slot("stock",["stock"],"tube-s",(0,0.03,-0.12),(0,0,0),"stock_smg_fold",True),
          slot("magazine",["magazine"],"mag-s45",(0,-0.025,0.09),(0,0,0),"mag_s_std",True),
          slot("cosmetic",["cosmetic"],"",(-0.026,0.0,-0.08))],
 "parts":s})
# ---------- Fusil d'assaut ----------
a = [box("upper","body",(0,0.03,0.0),(0.05,0.06,0.28)),
     box("lower","body",(0,-0.015,-0.01),(0.046,0.04,0.22)),
     box("handguard","body",(0,0.03,0.25),(0.056,0.058,0.24)),
     box("top_rail","metal",(0,0.064,0.1),(0.024,0.01,0.46)),
     cz("barrel","metal",(0,0.03,0.43),0.018,0.12),
     box("front_sight","metal",(0,0.08,0.35),(0.006,0.024,0.008)),
     box("rear_sight","metal",(0,0.078,-0.1),(0.02,0.018,0.02)),
     box("grip","polymer",(0,-0.075,-0.06),(0.034,0.1,0.045),(18,0,0)),
     box("charging_handle","metal",(0,0.055,-0.13),(0.03,0.01,0.03)),
     box("ejection_port","metal",(0.0255,0.035,0.0),(0.002,0.022,0.06)),
     box("forward_assist","metal",(0.03,0.04,-0.06),(0.012,0.014,0.02)),
     cz("buffer_tube","metal",(0,0.02,-0.22),0.032,0.16),
     box("magwell","body",(0,-0.05,0.06),(0.04,0.04,0.07))]
a += [box("hg_slot_l%d"%i,"metal",(-0.0285,0.03,0.17+i*0.05),(0.002,0.014,0.03)) for i in range(4)]
a += [box("hg_slot_r%d"%i,"metal",(0.0285,0.03,0.17+i*0.05),(0.002,0.014,0.03)) for i in range(4)]
a += trigger_group(0.005,-0.03,0.07)
weapons.append({"id":"talon_ar7","displayName":"Talon AR-7","family":"rifle","fireMode":"auto","pellets":1,
 "description":"Fusil d'assaut modulaire polyvalent, avec rail supérieur complet. La plateforme idéale pour expérimenter.",
 "price":5500,"unlockLevel":4,"modelId":"","baseStats":stats(32,65,40,650,30,60,2.4),
 "slots":[slot("optic",["optic"],"rail-t",(0,0.069,-0.03)),
          slot("muzzle",["muzzle"],"filetage-r",(0,0.03,0.49),(0,0,0),"muzzle_std_r"),
          slot("underbarrel",["grip"],"rail-t",(0,0.001,0.27)),
          slot("rail",["light","laser"],"rail-t",(0.028,0.03,0.3),(0,0,-90)),
          slot("stock",["stock"],"tube-s",(0,0.02,-0.16),(0,0,0),"stock_ar_std",True),
          slot("magazine",["magazine"],"mag-r30",(0,-0.07,0.06),(0,0,0),"mag_r_std",True),
          slot("cosmetic",["cosmetic"],"",(-0.026,-0.01,-0.11))],
 "parts":a})
# ---------- Fusil à pompe ----------
g = [box("receiver","body",(0,0.03,0),(0.05,0.07,0.24)),
     cz("barrel","metal",(0,0.05,0.35),0.024,0.5),
     cz("mag_tube","metal",(0,0.012,0.3),0.026,0.42),
     box("pump","polymer",(0,0.012,0.3),(0.046,0.042,0.16)),
     box("top_rail","metal",(0,0.068,0.0),(0.02,0.008,0.12)),
     sph("bead","metal",(0,0.066,0.59),0.008),
     box("grip","polymer",(0,-0.06,-0.1),(0.034,0.09,0.045),(20,0,0)),
     box("loading_port","metal",(0,-0.005,0.05),(0.03,0.002,0.08)),
     box("ejection_port","metal",(0.0255,0.04,0.02),(0.002,0.022,0.07))]
g += [box("pump_rib_%d"%i,"rubber",(0,-0.0095,0.25+i*0.025),(0.047,0.003,0.008)) for i in range(5)]
g += trigger_group(-0.02,-0.012,0.06)
weapons.append({"id":"breaker_12","displayName":"Breaker 12","family":"shotgun","fireMode":"pump","pellets":8,
 "description":"Fusil à pompe robuste. Dévastateur à courte portée, exigeant à longue distance.",
 "price":4200,"unlockLevel":5,"modelId":"","baseStats":stats(14,30,70,70,6,55,3.6),
 "slots":[slot("optic",["optic"],"rail-t",(0,0.072,0.0)),
          slot("muzzle",["muzzle"],"choke-12",(0,0.05,0.6)),
          slot("rail",["light","laser"],"rail-t",(0.024,0.012,0.3),(0,0,-90)),
          slot("stock",["stock"],"crosse-12",(0,0.02,-0.12),(0,0,0),"stock_sg_std",True),
          slot("cosmetic",["cosmetic"],"",(-0.026,0.0,-0.09))],
 "parts":g})
# ---------- Fusil de précision ----------
n = [cz("receiver","metal",(0,0.03,0.0),0.042,0.3),
     box("chassis","body",(0,0.0,0.05),(0.058,0.05,0.4)),
     box("forend","body",(0,0.005,0.33),(0.06,0.05,0.18)),
     cz("barrel","metal",(0,0.03,0.5),0.024,0.46),
     cx("bolt_handle","metal",(0.04,0.03,-0.09),0.01,0.05),
     sph("bolt_knob","metal",(0.068,0.026,-0.09),0.02),
     box("top_rail","metal",(0,0.058,0.0),(0.022,0.01,0.28)),
     box("grip","polymer",(0,-0.06,-0.13),(0.034,0.1,0.046),(22,0,0)),
     box("chassis_rear","body",(0,0.0,-0.13),(0.05,0.05,0.07))]
n += [box("forend_slot_%d"%i,"metal",(0.0305,0.005,0.27+i*0.04),(0.002,0.018,0.025)) for i in range(4)]
n += trigger_group(-0.06,-0.02,0.06)
weapons.append({"id":"longreach_sr12","displayName":"Longreach SR-12","family":"sniper","fireMode":"bolt","pellets":1,
 "description":"Fusil de précision à verrou sur châssis. Lent, lourd, mais d'une précision redoutable.",
 "price":8500,"unlockLevel":6,"modelId":"","baseStats":stats(110,92,75,45,5,30,3.2),
 "slots":[slot("optic",["optic"],"rail-t",(0,0.063,0.0),(0,0,0),"optic_scope_4x"),
          slot("muzzle",["muzzle"],"filetage-l",(0,0.03,0.73)),
          slot("underbarrel",["grip"],"rail-t",(0,-0.02,0.34)),
          slot("stock",["stock"],"chassis-l",(0,0.0,-0.165),(0,0,0),"stock_sniper_std",True),
          slot("magazine",["magazine"],"mag-l10",(0,-0.025,0.0),(0,0,0),"mag_l_std",True),
          slot("cosmetic",["cosmetic"],"",(-0.03,-0.01,-0.12))],
 "parts":n})

# ================= Accessoires =================
def att(id,name,type,price,lvl,mounts,parts,mods=(),families=(),conflicts=(),hides=(),desc=""):
    return {"id":id,"displayName":name,"type":type,"description":desc,"price":price,"unlockLevel":lvl,"mounts":list(mounts),
            "families":list(families),"conflicts":list(conflicts),"hidesParts":list(hides),
            "modifiers":[{"stat":m[0],"add":m[1],"mul":m[2] if len(m)>2 else 1.0} for m in mods],"modelId":"","parts":parts}
A=[]
A.append(att("optic_mini_dot","Point rouge MD-1","optic",400,1,["plaque-p","plaque-r","rail-t"],
  [box("body","metal",(0,0.008,0),(0.022,0.014,0.032)),box("hood_l","metal",(-0.0095,0.022,0.004),(0.003,0.016,0.022)),
   box("hood_r","metal",(0.0095,0.022,0.004),(0.003,0.016,0.022)),box("hood_top","metal",(0,0.0305,0.004),(0.022,0.003,0.022)),
   box("lens","glass",(0,0.022,0.012),(0.016,0.014,0.002)),sph("dot","emissive",(0,0.022,0.0),0.002)],
  [("accuracy",6),("handling",-2)],hides=["rear_sight"],desc="Viseur point rouge ultra-compact pour plaque de pistolet ou rail."))
A.append(att("optic_reflex_r1","Reflex R1","optic",900,2,["rail-t"],
  [box("base","metal",(0,0.008,0),(0.026,0.016,0.05)),cz("tube","body",(0,0.032,0),0.036,0.05),
   cz("lens","glass",(0,0.032,0.0),0.03,0.052),sph("dot","emissive",(0,0.032,-0.026),0.003)],
  [("accuracy",8),("handling",-3)],families=["smg","rifle","shotgun"],hides=["rear_sight"],desc="Viseur tubulaire à point lumineux, acquisition rapide."))
A.append(att("optic_holo_h2","Holographique H2","optic",1400,3,["rail-t"],
  [box("base","metal",(0,0.01,0),(0.034,0.02,0.07)),box("post_l","body",(-0.017,0.036,0.012),(0.004,0.032,0.046)),
   box("post_r","body",(0.017,0.036,0.012),(0.004,0.032,0.046)),box("roof","body",(0,0.053,0.012),(0.038,0.004,0.046)),
   box("window","glass",(0,0.036,0.03),(0.03,0.028,0.002)),box("buttons","rubber",(0.012,0.016,-0.03),(0.008,0.006,0.012))],
  [("accuracy",10),("handling",-4)],families=["smg","rifle","shotgun"],hides=["rear_sight"],desc="Fenêtre large et réticule holographique fictif."))
def scope(id,name,price,lvl,tube_len,obj_d,mods,fam):
    return att(id,name,"optic",price,lvl,["rail-t"],
      [box("ring_front","metal",(0,0.014,0.035),(0.022,0.028,0.012)),box("ring_rear","metal",(0,0.014,-0.035),(0.022,0.028,0.012)),
       cz("tube","body",(0,0.038,0),0.03,tube_len),cz("objective","body",(0,0.038,tube_len/2+0.02),obj_d,0.05),
       cz("eyepiece","body",(0,0.038,-tube_len/2-0.015),0.038,0.04),cy("turret_top","metal",(0,0.058,0),0.02,0.018),
       cx("turret_side","metal",(0.02,0.038,0),0.02,0.018),cz("lens","glass",(0,0.038,tube_len/2+0.046),obj_d*0.85,0.002)],
      mods,families=fam,hides=["rear_sight","front_sight"],desc="Lunette grossissante fictive.")
A.append(scope("optic_scope_4x","Lunette ML-4x",2200,4,0.14,0.044,[("accuracy",16),("handling",-10)],["smg","rifle","sniper","shotgun"]))
A.append(scope("optic_scope_8x","Lunette LR-8x",3500,6,0.22,0.056,[("accuracy",24),("handling",-16),("recoil",-2)],["rifle","sniper"]))
A.append(att("muzzle_std_r","Cache-flamme standard","muzzle",0,1,["filetage-r"],
  [cz("body","metal",(0,0,0.03),0.022,0.06)]+[box("slot_%d"%i,"polymer",(0,0.0,0.035),(0.023,0.004,0.03),(0,0,i*60)) for i in range(3)],
  [],desc="Cache-flamme d'origine."))
A.append(att("muzzle_comp_c1","Compensateur C1","muzzle",700,2,["filetage-r","filetage-s","filetage-l"],
  [cz("body","metal",(0,0,0.03),0.027,0.06),box("port_1","polymer",(0,0.013,0.02),(0.014,0.003,0.007)),
   box("port_2","polymer",(0,0.013,0.04),(0.014,0.003,0.007))],
  [("recoil",-10),("accuracy",-2)],desc="Réduit la remontée de l'arme (effet de jeu)."))
A.append(att("muzzle_supp_s3","Modérateur S3","muzzle",2000,3,["filetage-r","filetage-s","filetage-l"],
  [cz("cap_rear","metal",(0,0,0.005),0.03,0.01),cz("body","body",(0,0,0.09),0.036,0.16),cz("cap_front","metal",(0,0,0.172),0.034,0.008)],
  [("recoil",-6),("accuracy",4),("handling",-8),("damage",0,0.95)],desc="Atténue le bruit et la signature du tir dans le jeu."))
A.append(att("muzzle_brake_b2","Frein de bouche B2","muzzle",1200,4,["filetage-r","filetage-l"],
  [box("body","metal",(0,0,0.03),(0.04,0.026,0.06)),box("baffle_1","polymer",(0,0,0.018),(0.042,0.018,0.006)),
   box("baffle_2","polymer",(0,0,0.04),(0.042,0.018,0.006))],
  [("recoil",-16),("handling",-3)],desc="Gros réducteur de recul pour plateformes longues."))
A.append(att("muzzle_choke_x","Choke serré X","muzzle",600,5,["choke-12"],
  [cz("body","metal",(0,0,0.02),0.03,0.04),cz("ring","accent",(0,0,0.03),0.032,0.006)],
  [("accuracy",12),("handling",-1)],families=["shotgun"],desc="Resserre la gerbe de plombs virtuels."))
A.append(att("grip_vertical","Poignée verticale V1","grip",600,2,["rail-t"],
  [box("clamp","metal",(0,-0.006,0),(0.024,0.012,0.04)),cy("handle","polymer",(0,-0.05,0),0.03,0.08),
   cy("cap","rubber",(0,-0.091,0),0.032,0.004)],
  [("recoil",-8),("handling",-2)],desc="Stabilise le tir soutenu."))
A.append(att("grip_angled","Poignée inclinée A2","grip",800,3,["rail-t"],
  [box("clamp","metal",(0,-0.006,0),(0.024,0.012,0.07)),box("ramp","polymer",(0,-0.022,0.0),(0.026,0.03,0.07),(-20,0,0))],
  [("recoil",-4),("handling",4)],conflicts=["light_l2"],desc="Améliore la prise en main et la vitesse de visée."))
A.append(att("grip_bipod","Bipied BP-1","grip",1500,6,["rail-t"],
  [box("clamp","metal",(0,-0.008,0),(0.03,0.016,0.04)),cy("leg_l","metal",(-0.012,-0.02,0.08),0.01,0.16,(90,0,0)),
   cy("leg_r","metal",(0.012,-0.02,0.08),0.01,0.16,(90,0,0)),sph("foot_l","rubber",(-0.012,-0.02,0.16),0.014),sph("foot_r","rubber",(0.012,-0.02,0.16),0.014)],
  [("accuracy",10),("handling",-12)],families=["rifle","sniper"],desc="Bipied replié ; stabilité maximale en position."))
A.append(att("grip_rev_wood","Plaquettes bois","grip",0,1,["crosse-r"],
  [box("grip","wood",(0,0.0,0),(0.034,0.085,0.042)),box("medallion","accent",(0.0172,0.01,0),(0.001,0.012,0.012))],[],desc="Plaquettes d'origine en bois verni."))
A.append(att("grip_rev_rubber","Poignée caoutchouc","grip",500,2,["crosse-r"],
  [box("grip","rubber",(0,0.0,0),(0.036,0.088,0.044))]+[box("groove_%d"%i,"polymer",(0,0.02-i*0.02,0.0225),(0.034,0.004,0.002)) for i in range(3)],
  [("recoil",-8),("handling",2)],desc="Absorbe le recul, meilleure tenue."))
def ar_stock(id,name,price,lvl,parts,mods,mounts=("tube-s",),fam=(),desc=""):
    return att(id,name,"stock",price,lvl,list(mounts),parts,mods,families=fam,desc=desc)
A.append(ar_stock("stock_ar_std","Crosse réglable standard",0,1,
  [box("body","polymer",(0,-0.005,-0.09),(0.04,0.07,0.15)),box("pad","rubber",(0,-0.005,-0.171),(0.042,0.08,0.012)),
   box("cheek","polymer",(0,0.032,-0.08),(0.034,0.012,0.11))],[],fam=["rifle"],desc="Crosse d'origine à 6 positions (fictive)."))
A.append(ar_stock("stock_light","Crosse squelette SK",900,2,
  [box("rail_top","metal",(0,0.02,-0.08),(0.01,0.01,0.16)),box("rail_low","metal",(0,-0.025,-0.09),(0.01,0.01,0.15),(-12,0,0)),
   box("pad","rubber",(0,-0.005,-0.165),(0.03,0.075,0.01))],[("handling",10),("recoil",6)],fam=["rifle","smg"],desc="Ultra-légère : maniabilité contre stabilité."))
A.append(ar_stock("stock_precision","Crosse précision PR",1600,4,
  [box("body","polymer",(0,-0.012,-0.13),(0.045,0.09,0.24)),box("riser","body",(0,0.045,-0.12),(0.04,0.02,0.14)),
   box("pad","rubber",(0,-0.012,-0.255),(0.047,0.1,0.014))],[("recoil",-10),("accuracy",6),("handling",-8)],
  mounts=("tube-s","chassis-l"),fam=["rifle","sniper"],desc="Crosse réglable lourde pour le tir de précision."))
A.append(ar_stock("stock_smg_fold","Crosse pliante K",0,1,
  [box("rod_l","metal",(-0.014,0,-0.1),(0.006,0.006,0.2)),box("rod_r","metal",(0.014,0,-0.1),(0.006,0.006,0.2)),
   box("plate","rubber",(0,-0.01,-0.2),(0.036,0.06,0.01))],[],fam=["smg"],desc="Crosse pliante d'origine."))
A.append(ar_stock("stock_sniper_std","Crosse fixe LR",0,1,
  [box("body","body",(0,-0.02,-0.16),(0.05,0.1,0.3)),box("cheek","body",(0,0.04,-0.15),(0.044,0.02,0.16)),
   box("pad","rubber",(0,-0.02,-0.316),(0.052,0.11,0.012)),box("hook","polymer",(0,-0.075,-0.25),(0.03,0.02,0.06))],
  [],mounts=("chassis-l",),fam=["sniper"],desc="Crosse fixe d'origine."))
A.append(ar_stock("stock_sg_std","Crosse bois B12",0,1,
  [box("body","wood",(0,-0.03,-0.15),(0.044,0.085,0.3),(8,0,0)),box("pad","rubber",(0,-0.05,-0.302),(0.046,0.095,0.012),(8,0,0))],
  [],mounts=("crosse-12",),fam=["shotgun"],desc="Crosse en bois d'origine."))
A.append(ar_stock("stock_sg_synth","Crosse synthétique B12-S",700,5,
  [box("body","polymer",(0,-0.03,-0.15),(0.044,0.085,0.3),(8,0,0)),box("pad","rubber",(0,-0.05,-0.305),(0.048,0.1,0.02),(8,0,0)),
   box("shell_holder","metal",(0.023,-0.02,-0.12),(0.002,0.03,0.08),(8,0,0))],
  [("recoil",-6),("handling",2)],mounts=("crosse-12",),fam=["shotgun"],desc="Crosse synthétique avec talon amortissant."))
def mag(id,name,price,lvl,mount,parts,mods,desc):
    return att(id,name,"magazine",price,lvl,[mount],parts,mods,desc=desc)
A.append(mag("mag_p9_std","Chargeur P9 (15)",0,1,"mag-p9",[box("baseplate","polymer",(0,-0.054,0),(0.03,0.008,0.044))],[],"Chargeur standard."))
A.append(mag("mag_p9_ext","Chargeur P9 étendu (21)",500,1,"mag-p9",
  [box("body","metal",(0,-0.066,0),(0.024,0.026,0.036)),box("baseplate","polymer",(0,-0.082,0),(0.03,0.008,0.044))],
  [("magazineSize",6),("handling",-3),("reloadTime",0.15)],"Six cartouches virtuelles de plus."))
A.append(mag("mag_r_std","Chargeur R30",0,1,"mag-r30",[box("body","metal",(0,-0.06,0.012),(0.028,0.13,0.062),(-12,0,0)),
  box("baseplate","polymer",(0,-0.125,0.026),(0.032,0.01,0.068),(-12,0,0))],[],"Chargeur courbe standard."))
A.append(mag("mag_r_ext","Chargeur R45",1100,4,"mag-r30",[box("body","metal",(0,-0.085,0.02),(0.028,0.18,0.062),(-16,0,0)),
  box("baseplate","polymer",(0,-0.172,0.045),(0.032,0.01,0.068),(-16,0,0))],
  [("magazineSize",15),("handling",-6),("reloadTime",0.35)],"Grande capacité, plus encombrant."))
A.append(mag("mag_s_std","Chargeur S30",0,1,"mag-s45",[box("body","metal",(0,-0.075,0),(0.026,0.15,0.038)),
  box("baseplate","polymer",(0,-0.152,0),(0.03,0.008,0.042))],[],"Chargeur droit standard."))
A.append(mag("mag_s_ext","Chargeur S50",900,3,"mag-s45",[box("body","metal",(0,-0.11,0),(0.026,0.22,0.038)),
  box("baseplate","polymer",(0,-0.222,0),(0.03,0.008,0.042))],[("magazineSize",20),("handling",-5),("reloadTime",0.3)],"Capacité accrue."))
A.append(mag("mag_l_std","Chargeur L5",0,1,"mag-l10",[box("body","metal",(0,-0.022,0),(0.034,0.045,0.09))],[],"Chargeur 5 coups."))
A.append(mag("mag_l_ext","Chargeur L10",1200,6,"mag-l10",[box("body","metal",(0,-0.045,0),(0.034,0.09,0.09))],
  [("magazineSize",5),("handling",-4),("reloadTime",0.3)],"Chargeur 10 coups."))
A.append(att("light_l1","Lampe tactique L1","light",450,1,["rail-t","rail-c"],
  [box("clamp","metal",(0,0.005,0.0),(0.02,0.01,0.03)),cz("body","body",(0,0.018,0.012),0.022,0.055),
   cz("lens","lamp",(0,0.018,0.04),0.018,0.002)],[("handling",-1)],desc="Lampe compacte (éclairage virtuel dans les missions)."))
A.append(att("light_l2","Lampe longue portée L2","light",900,3,["rail-t"],
  [box("clamp","metal",(0,0.005,0.0),(0.022,0.01,0.04)),cz("body","body",(0,0.022,0.02),0.026,0.08),
   cz("head","body",(0,0.022,0.065),0.034,0.02),cz("lens","lamp",(0,0.022,0.076),0.03,0.002)],
  [("handling",-3)],conflicts=["grip_angled"],desc="Faisceau puissant et long."))
A.append(att("laser_v1","Laser virtuel V1","laser",700,2,["rail-t","rail-c"],
  [box("body","polymer",(0,0.012,0.012),(0.02,0.022,0.04)),sph("aperture","emissive",(0,0.012,0.033),0.006),
   box("switch","rubber",(0,0.012,-0.01),(0.012,0.012,0.006))],
  [("accuracy",4)],desc="Pointeur laser simulé : améliore le tir sans visée."))
A.append(att("charm_tag","Plaque d'identité","cosmetic",150,1,[],
  [cy("ring","metal",(0,-0.005,0),0.012,0.002,(0,0,90)),box("plate","accent",(0,-0.03,0),(0.002,0.03,0.02))],[],desc="Petite plaque métallique personnalisable."))
A.append(att("charm_die","Dé porte-bonheur","cosmetic",250,2,[],
  [cy("ring","metal",(0,-0.005,0),0.01,0.002,(0,0,90)),box("die","emissive",(0,-0.025,0),(0.016,0.016,0.016),(20,30,10))],[],desc="Un dé rouge pour les tireurs superstitieux."))

F=[]
def fin(id,name,price,lvl,c,m,s): F.append({"id":id,"displayName":name,"price":price,"unlockLevel":lvl,"color":{"r":c[0],"g":c[1],"b":c[2],"a":1.0},"metallic":m,"smoothness":s})
fin("finish_graphite","Graphite",0,1,(0.085,0.09,0.095),0.45,0.5)
fin("finish_desert","Sable du désert",400,1,(0.55,0.45,0.32),0.1,0.35)
fin("finish_olive","Olive",400,2,(0.27,0.3,0.19),0.1,0.35)
fin("finish_gunmetal","Acier bruni",800,2,(0.3,0.32,0.35),0.85,0.7)
fin("finish_arctic","Arctique",900,3,(0.82,0.84,0.86),0.1,0.4)
fin("finish_cobalt","Cobalt",1200,4,(0.1,0.2,0.45),0.75,0.65)
fin("finish_copper","Cuivre poli",1500,5,(0.72,0.42,0.25),1.0,0.78)
fin("finish_crimson","Cramoisi",1500,6,(0.45,0.05,0.06),0.6,0.65)

eco = {"startingMoney":1500,"startingWeapons":["vk9_sentinel"],"startingAttachments":["charm_tag"],
       "startingFinishes":["finish_graphite"],"defaultFinish":"finish_graphite","xpBase":400,"xpGrowth":1.35,"maxLevel":30,"maxBuildsPerWeapon":6}
os.makedirs(OUT, exist_ok=True)
def dump(name,obj):
    with open(os.path.join(OUT,name),"w",encoding="utf-8") as f: json.dump(obj,f,ensure_ascii=False,indent=1); f.write("\n")
dump("weapons.json",{"weapons":weapons}); dump("attachments.json",{"attachments":A}); dump("finishes.json",{"finishes":F}); dump("economy.json",eco)
print(len(weapons),len(A),len(F))
