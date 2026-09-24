from pathlib import Path
from reportlab.pdfgen import canvas
from reportlab.lib.pagesizes import landscape, letter
from reportlab.lib.colors import HexColor, white
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "output" / "pdf" / "Averion_Compass_Design_Samples.pdf"
OUT.parent.mkdir(parents=True, exist_ok=True)

FONT = r"C:\Windows\Fonts\segoeui.ttf"
FONT_BOLD = r"C:\Windows\Fonts\segoeuib.ttf"
FONT_LIGHT = r"C:\Windows\Fonts\segoeuil.ttf"
pdfmetrics.registerFont(TTFont("Segoe", FONT))
pdfmetrics.registerFont(TTFont("SegoeBold", FONT_BOLD))
pdfmetrics.registerFont(TTFont("SegoeLight", FONT_LIGHT))

PAGE_W, PAGE_H = landscape(letter)

GOLD = {
    "paper": "#F2F3F4", "surface": "#FFFFFF", "ink": "#151B22", "navy": "#101820",
    "navy2": "#222C36", "accent": "#C3A36A", "accent2": "#E2D1AD", "silver": "#AAB1B8",
    "line": "#D9DDE0", "muted": "#68727C", "green": "#557A69", "red": "#A75F53",
}
BLUE = {
    "paper": "#F2F8FA", "surface": "#FFFFFF", "ink": "#123B61", "navy": "#123B61",
    "navy2": "#0B4F80", "accent": "#11B8C9", "accent2": "#C7F0F3", "silver": "#71AEC2",
    "line": "#CFE3EA", "muted": "#58758D", "green": "#79C943", "red": "#D86F53",
}
LEGACY = {
    "paper": "#F4FBFD", "surface": "#FFFFFF", "ink": "#123B61", "navy": "#123B61",
    "navy2": "#0B4F80", "accent": "#11B8C9", "accent2": "#D9F7FB", "silver": "#11B8C9",
    "line": "#CFE3EA", "muted": "#58758D", "green": "#79C943", "red": "#D86F53",
}

def col(p, key):
    return HexColor(p[key])

def text(c, s, x, y, size=8, color="#151B22", font="Segoe"):
    c.setFillColor(HexColor(color))
    c.setFont(font, size)
    c.drawString(x, y, s)

def right_text(c, s, x, y, size=8, color="#151B22", font="Segoe"):
    c.setFillColor(HexColor(color))
    c.setFont(font, size)
    c.drawRightString(x, y, s)

def line(c, x1, y1, x2, y2, color, width=0.6):
    c.setStrokeColor(HexColor(color)); c.setLineWidth(width); c.line(x1, y1, x2, y2)

def logo(c, x, y, size, p):
    # A flowing, ribbon-like A mark based on the supplied Averion board.
    q = c.beginPath(); q.moveTo(x+size*.04,y+size*.08)
    q.curveTo(x+size*.24,y+size*.48,x+size*.38,y+size*.97,x+size*.55,y+size*.95)
    q.curveTo(x+size*.72,y+size*.93,x+size*.83,y+size*.43,x+size*.98,y+size*.08)
    q.lineTo(x+size*.72,y+size*.08); q.curveTo(x+size*.64,y+size*.33,x+size*.58,y+size*.55,x+size*.5,y+size*.62)
    q.curveTo(x+size*.43,y+size*.56,x+size*.35,y+size*.32,x+size*.26,y+size*.08); q.close()
    c.setFillColor(HexColor(p["silver"])); c.drawPath(q, fill=1, stroke=0)
    line(c, x+size*.42, y+size*.38, x+size*.63, y+size*.38, p["accent"], max(.7,size*.018))

def round_box(c, x, y, w, h, fill, stroke=None, radius=5, sw=.6):
    c.setFillColor(HexColor(fill)); c.setStrokeColor(HexColor(stroke or fill)); c.setLineWidth(sw)
    c.roundRect(x,y,w,h,radius,fill=1,stroke=1 if stroke else 0)

def draw_small_dashboard(c, x, y, w, h, p):
    round_box(c,x,y,w,h,p["surface"],p["line"],5,.6)
    # top bar
    c.setFillColor(HexColor(p["navy2"])); c.roundRect(x+1,y+h-24,w-2,23,4,fill=1,stroke=0)
    logo(c,x+8,y+h-19,12,p)
    text(c,"AVERION COMPASS",x+25,y+h-15,5.6,"#FFFFFF","SegoeBold")
    right_text(c,"All properties   Alex M.",x+w-8,y+h-15,5.2,"#E6EBEF","Segoe")
    # left navigation rail
    rail=max(54,w*.22)
    c.setFillColor(HexColor(p["navy"])); c.rect(x+1,y+1,rail,h-25,fill=1,stroke=0)
    navs=["Portfolio","Properties","Leasing","Residents","Work orders","Payments","Reports","Autopilot"]
    for i,n in enumerate(navs):
        yy=y+h-43-i*15
        if i==0:
            c.setFillColor(HexColor(p["navy2"])); c.roundRect(x+5,yy-4,rail-10,12,3,fill=1,stroke=0)
            c.setFillColor(HexColor(p["accent"])); c.rect(x+5,yy-4,2,12,fill=1,stroke=0)
        c.setFillColor(HexColor("#E8ECEF" if i==0 else "#AEB7BF")); c.setFont("Segoe",5.3); c.drawString(x+12,yy,n)
    # content area
    cx=x+rail+9; cw=w-rail-18; top=y+h-38
    text(c,"Your portfolio, in rhythm.",cx,top-8,10,p["ink"],"SegoeBold")
    text(c,"Three things need attention. The rest is moving as planned.",cx,top-18,5.1,p["muted"])
    # four summary cards
    gap=4; kw=(cw-3*gap)/4; ky=top-47
    vals=[("OCCUPANCY","94.8%"),("RENT COLLECTED","$482k"),("WORK IN MOTION","28"),("LEASES TO RENEW","16")]
    for i,(lab,val) in enumerate(vals):
        xx=cx+i*(kw+gap); round_box(c,xx,ky,kw,24,p["surface"],p["line"],3,.4)
        text(c,lab,xx+4,ky+15,3.8,p["muted"],"SegoeBold")
        text(c,val,xx+4,ky+4,8,p["ink"],"SegoeBold")
        c.setFillColor(HexColor(p["accent"])); c.rect(xx+1,ky+1,kw*.43,1,fill=1,stroke=0)
    # attention timeline
    lower=ky-8; leftw=cw*.57; rightx=cx+leftw+5; rightw=cw-leftw-5
    round_box(c,cx,lower-90,leftw,86,p["surface"],p["line"],3,.4)
    text(c,"NEEDS YOUR ATTENTION",cx+7,lower-13,5.7,p["ink"],"SegoeBold")
    text(c,"PRIORITIZED ACROSS 12 PROPERTIES",cx+7,lower-21,3.8,p["muted"])
    items=[("NOW","HVAC - Riverside"),("TODAY","Renewal - Maple"),("TODAY","Invoice - Lakeside"),("FRI","Move-in - Oakwood")]
    for i,(when,label) in enumerate(items):
        yy=lower-34-i*13
        line(c,cx+7,yy-4,cx+leftw-7,yy-4,p["line"],.35)
        c.setFillColor(HexColor(p["accent"])); c.circle(cx+11,yy,2.2,fill=1,stroke=0)
        text(c,when,cx+17,yy+1,4.2,p["muted"],"SegoeBold")
        text(c,label,cx+42,yy+1,4.4,p["ink"])
    # property network
    round_box(c,rightx,lower-54,rightw,50,p["surface"],p["line"],3,.4)
    text(c,"PROPERTY NETWORK",rightx+6,lower-14,5.3,p["ink"],"SegoeBold")
    text(c,"12 COMMUNITIES - 2,840 HOMES",rightx+6,lower-22,3.8,p["muted"])
    nodes=[(rightx+15,lower-39,"Riverside"),(rightx+rightw*.42,lower-34,"Maple"),(rightx+rightw*.74,lower-41,"Lakeside"),(rightx+rightw*.54,lower-48,"Oakwood")]
    c.setStrokeColor(HexColor(p["silver"])); c.setLineWidth(.8)
    for a,b in [((nodes[0][0],nodes[0][1]),(nodes[1][0],nodes[1][1])),((nodes[1][0],nodes[1][1]),(nodes[2][0],nodes[2][1])),((nodes[1][0],nodes[1][1]),(nodes[3][0],nodes[3][1]))]: c.line(a[0],a[1],b[0],b[1])
    for nx,ny,nl in nodes:
        c.setFillColor(HexColor(p["surface"])); c.setStrokeColor(HexColor(p["accent"])); c.circle(nx,ny,4,fill=1,stroke=1)
        text(c,nl,nx-10,ny-9,3.4,p["muted"])
    # bottom shortcuts
    for i,label in enumerate(["Create work order","Review renewals","Open daily briefing"]):
        xx=cx+i*(cw/3); round_box(c,xx, y+8,cw/3-3,13,p["paper"],p["line"],2,.35); text(c,label,xx+5,y+13,4.2,p["ink"],"SegoeBold")

def draw_new_website(c,x,y,w,h,p,more_gold=False):
    round_box(c,x,y,w,h,p["surface"],p["line"],5,.6)
    # nav
    c.setFillColor(HexColor("#FAFAFA")); c.rect(x+1,y+h-25,w-2,24,fill=1,stroke=0)
    logo(c,x+10,y+h-20,13,p); text(c,"AVERION",x+27,y+h-13,6.7,p["ink"],"SegoeBold"); text(c,"SOFTWARE",x+27,y+h-19,3.5,p["muted"])
    text(c,"PRODUCTS   INDUSTRIES   COMPANY",x+w*.48,y+h-15,4.5,p["muted"],"SegoeBold")
    round_box(c,x+w-64,y+h-20,54,12,p["navy"],None,2); text(c,"LET'S TALK",x+w-53,y+h-16,4.1,"#FFFFFF","SegoeBold")
    # hero background
    hero_y=y+83; hero_h=h-109
    c.setFillColor(HexColor(p["paper"])); c.rect(x+1,hero_y,w-2,hero_h,fill=1,stroke=0)
    # restrained flowing lines echo the Averion mark without competing with copy
    q=c.beginPath(); q.moveTo(x+w*.62,hero_y+hero_h-4); q.curveTo(x+w*.77,hero_y+hero_h+4,x+w*.82,hero_y+18,x+w*.98,hero_y+7)
    c.setStrokeColor(HexColor(p["silver"])); c.setLineWidth(.7); c.drawPath(q,fill=0,stroke=1)
    q=c.beginPath(); q.moveTo(x+w*.7,hero_y+hero_h-8); q.curveTo(x+w*.82,hero_y+hero_h-2,x+w*.89,hero_y+28,x+w*.99,hero_y+20)
    c.setStrokeColor(HexColor(p["accent"] if more_gold else p["silver"])); c.setLineWidth(1.2); c.drawPath(q,fill=0,stroke=1)
    text(c,"THE FUTURE OF SOFTWARE BUILDS WITH YOU",x+13,hero_y+hero_h-20,4.2,p["muted"],"SegoeBold")
    text(c,"One platform.",x+13,hero_y+hero_h-45,16,p["ink"],"SegoeBold")
    text(c,"Always evolving.",x+13,hero_y+hero_h-63,14,p["accent"] if more_gold else p["muted"],"SegoeLight")
    text(c,"Purpose-built software that grows with your business.",x+13,hero_y+hero_h-79,5.3,p["muted"])
    text(c,"Connected products. A consistent experience. A partner for what's next.",x+13,hero_y+hero_h-88,4.5,p["muted"])
    btn=p["accent"] if more_gold else p["navy"]
    round_box(c,x+13,hero_y+14,85,15,btn,None,2)
    text(c,"EXPLORE AVERION COMPASS",x+18,hero_y+19,4.1,"#FFFFFF" if btn in [p["navy"],p["navy2"]] else p["ink"],"SegoeBold")
    # metallic mark and overlay card
    logo(c,x+w*.82,hero_y+58,25,p)
    cardx=x+w*.63; cardy=hero_y+8; cardw=w*.33; cardh=46
    round_box(c,cardx,cardy,cardw,cardh,p["surface"],p["line"],3,.5)
    text(c,"AVERION COMPASS",cardx+7,cardy+cardh-11,3.7,p["muted"],"SegoeBold")
    text(c,"Every property, in step.",cardx+7,cardy+cardh-22,6,p["ink"],"SegoeBold")
    text(c,"Portfolio occupancy",cardx+7,cardy+13,3.7,p["muted"])
    text(c,"94.8%",cardx+7,cardy+4,10,p["ink"],"SegoeBold")
    line(c,cardx+40,cardy+8,cardx+cardw-5,cardy+16,p["accent"],1)
    # advantage bar
    bar_y=y+59; c.setFillColor(HexColor(p["navy"])); c.rect(x+1,bar_y,w-2,24,fill=1,stroke=0)
    adv=[("FAST","Go further, faster."),("ALL IN ONE","Everything in one place."),("EVER CHANGING","Always improving."),("BUILT FOR WHAT'S NEXT","Tomorrow's opportunity.")]
    for i,(head,sub) in enumerate(adv):
        xx=x+10+i*(w-20)/4
        if i>0: line(c,xx-5,bar_y+4,xx-5,bar_y+20,"#58616A",.4)
        text(c,head,xx,bar_y+13,4.2,p["accent"],"SegoeBold"); text(c,sub,xx,bar_y+6,3.5,"#D6D9DC")
    # product family row
    for i,(label,sub) in enumerate([("AVERION COMPASS","REAL ESTATE - PROPERTY OPERATIONS"),("AVERION ATLAS","PORTFOLIO INTELLIGENCE"),("MORE TO COME","ONE EVOLVING PLATFORM")]):
        ww=(w-2)/3; xx=x+1+i*ww
        c.setFillColor(HexColor("#FBFBFB")); c.rect(xx,y+1,ww,57,fill=1,stroke=0)
        if i>0: line(c,xx,y+8,xx,y+51,p["line"],.4)
        logo(c,xx+8,y+36,13,p)
        text(c,label,xx+28,y+42,4.6,p["ink"],"SegoeBold")
        text(c,sub,xx+28,y+32,3.1,p["muted"])
        line(c,xx+8,y+14,xx+ww-8,y+14,p["accent"],.65)

def draw_legacy_website(c,x,y,w,h,p):
    round_box(c,x,y,w,h,p["surface"],p["line"],5,.6)
    c.setFillColor(HexColor("#FFFFFF")); c.rect(x+1,y+h-25,w-2,24,fill=1,stroke=0)
    # original-style wordmark replacement
    logo(c,x+10,y+h-20,13,p); text(c,"AVERION",x+27,y+h-13,6.2,p["navy2"],"SegoeBold"); text(c,"COMPASS",x+27,y+h-19,3.6,p["accent"],"SegoeBold")
    text(c,"PRODUCT   MOBILE   PRICING",x+w*.5,y+h-15,4.3,p["muted"])
    round_box(c,x+w-63,y+h-20,53,12,p["navy2"],None,2); text(c,"REQUEST A DEMO",x+w-57,y+h-16,3.5,"#FFFFFF","SegoeBold")
    hero_y=y+84; hero_h=h-110
    c.setFillColor(HexColor(p["paper"])); c.rect(x+1,hero_y,w-2,hero_h,fill=1,stroke=0)
    text(c,"PROPERTY OPERATIONS, MADE CLEAR",x+13,hero_y+hero_h-20,4.1,p["navy2"],"SegoeBold")
    c.setFillColor(HexColor(p["ink"])); c.setFont("Times-Bold",15); c.drawString(x+13,hero_y+hero_h-43,"One connected platform")
    c.setFont("Times-Italic",14); c.setFillColor(HexColor(p["accent"])); c.drawString(x+13,hero_y+hero_h-59,"for every property.")
    text(c,"Cove PM becomes Averion Compass - connected operations for",x+13,hero_y+hero_h-75,4.8,p["muted"])
    text(c,"maintenance, leasing, residents, payments, and reporting.",x+13,hero_y+hero_h-84,4.8,p["muted"])
    round_box(c,x+13,hero_y+12,77,15,p["navy2"],None,2); text(c,"EXPLORE COMPASS",x+20,hero_y+17,4.1,"#FFFFFF","SegoeBold")
    # legacy mock dashboard card
    dx=x+w*.56; dy=hero_y+8; dw=w*.4; dh=hero_h-15
    round_box(c,dx,dy,dw,dh,"#FFFFFF",p["line"],4,.5)
    text(c,"PROPERTY OVERVIEW",dx+9,dy+dh-14,4.1,p["muted"],"SegoeBold")
    text(c,"Portfolio snapshot",dx+9,dy+dh-27,8,p["ink"],"Times-Roman")
    for i,(lab,val) in enumerate([("OCCUPANCY","94.8%"),("OPEN REQUESTS","28"),("MOVE-INS","12")]):
        yy=dy+dh-52-i*24; round_box(c,dx+8,yy,dw-16,19,p["accent2"],None,2)
        text(c,lab,dx+14,yy+11,3.4,p["muted"],"SegoeBold"); text(c,val,dx+dw-34,yy+5,8,p["ink"],"SegoeBold")
    bar_y=y+59; c.setFillColor(HexColor(p["navy2"])); c.rect(x+1,bar_y,w-2,24,fill=1,stroke=0)
    text(c,"MAINTENANCE",x+13,bar_y+13,4.4,"#FFFFFF","SegoeBold"); text(c,"LEASING",x+w*.34,bar_y+13,4.4,"#FFFFFF","SegoeBold"); text(c,"RESIDENTS",x+w*.56,bar_y+13,4.4,"#FFFFFF","SegoeBold"); text(c,"REPORTING",x+w*.79,bar_y+13,4.4,"#FFFFFF","SegoeBold")
    for i,(a,b) in enumerate([("Connected work","Manage requests from intake to completion."),("Better resident service","Keep communication close to the work."),("Clear portfolio view","Track performance in one place.")]):
        xx=x+1+i*(w-2)/3; ww=(w-2)/3; c.setFillColor(HexColor("#FFFFFF")); c.rect(xx,y+1,ww,57,fill=1,stroke=0)
        if i>0: line(c,xx,y+7,xx,y+52,p["line"],.4)
        text(c,a,xx+8,y+40,5.2,p["ink"],"Times-Bold"); text(c,b,xx+8,y+27,3.8,p["muted"]); text(c,"LEARN MORE  >",xx+8,y+13,3.3,p["navy2"],"SegoeBold")

def draw_legacy_login(c,x,y,w,h,p):
    round_box(c,x,y,w,h,"#E8F3F6",p["line"],5,.6)
    cardw=w*.63; cardh=h-26; cx=x+(w-cardw)/2; cy=y+13
    round_box(c,cx,cy,cardw,cardh,"#FFFFFF","#D9E5E9",5,.6)
    logo(c,cx+cardw*.42,cy+cardh-48,25,p)
    text(c,"AVERION COMPASS",cx+cardw*.18,cy+cardh-63,6.3,p["navy2"],"SegoeBold")
    text(c,"Property operations software",cx+cardw*.18,cy+cardh-75,4.3,p["muted"])
    text(c,"Organization",cx+cardw*.18,cy+cardh-99,4.4,p["ink"],"SegoeBold")
    for i,(lab,placeholder) in enumerate([("","Your workspace"),("Email","name@company.com"),("Password","Enter password")]):
        yy=cy+cardh-121-i*34
        if lab: text(c,lab,cx+cardw*.18,yy+19,4.3,p["ink"],"SegoeBold")
        round_box(c,cx+cardw*.18,yy,cardw*.64,15,"#FFFFFF",p["line"],2,.5); text(c,placeholder,cx+cardw*.21,yy+5,3.7,p["muted"])
    round_box(c,cx+cardw*.18,cy+15,cardw*.64,18,p["navy2"],None,3)
    text(c,"SIGN IN",cx+cardw*.42,cy+21,4.6,"#FFFFFF","SegoeBold")
    text(c,"Forgot password?",cx+cardw*.18,cy+6,3.6,p["navy2"])

def draw_page(c, idx, title, subtitle, p, mode, gold=False):
    c.setFillColor(HexColor(p["paper"])); c.rect(0,0,PAGE_W,PAGE_H,fill=1,stroke=0)
    # heading
    logo(c,29,PAGE_H-46,19,p)
    text(c,"AVERION SOFTWARE",54,PAGE_H-31,6.3,p["ink"],"SegoeBold")
    text(c,"PRODUCT FAMILY - COMPASS / ATLAS",54,PAGE_H-41,4.3,p["muted"],"Segoe")
    right_text(c,f"DESIGN SAMPLE 0{idx} / 03",PAGE_W-29,PAGE_H-31,5.6,p["muted"],"SegoeBold")
    line(c,29,PAGE_H-52,PAGE_W-29,PAGE_H-52,p["line"],.6)
    text(c,title,29,PAGE_H-79,17,p["ink"],"SegoeBold")
    text(c,subtitle,29,PAGE_H-95,7,p["muted"],"Segoe")
    # sample labels
    text(c,"WEBSITE",29,PAGE_H-114,4.5,p["muted"],"SegoeBold")
    text(c,"APPLICATION",481,PAGE_H-114,4.5,p["muted"],"SegoeBold")
    panel_y=166; panel_h=318
    draw_panel_x=29; web_w=440; app_x=481; app_w=282
    if mode=="legacy": draw_legacy_website(c,draw_panel_x,panel_y,web_w,panel_h,p)
    else: draw_new_website(c,draw_panel_x,panel_y,web_w,panel_h,p,more_gold=gold)
    if mode=="legacy": draw_legacy_login(c,app_x,panel_y,app_w,panel_h,p)
    else: draw_small_dashboard(c,app_x,panel_y,app_w,panel_h,p)
    # bottom description and swatches
    line(c,29,146,PAGE_W-29,146,p["line"],.6)
    text(c,"DESIGN NOTES",29,132,4.5,p["muted"],"SegoeBold")
    if idx==1:
        notes=["Averion board direction with a stronger champagne-gold signature.","Gold is used for calls to action, focus states, data traces, and key moments.","The app keeps a bright working surface and dark navy navigation spine."]
    elif idx==2:
        notes=["Same layout, mark, spacing, and Segoe UI type as Sample 01.","Restores the CovePM palette: navy blue, cyan, and green.","Gold is replaced by cyan for emphasis and green for positive status."]
    else:
        notes=["Returns to the original CovePM website layout and Georgia headline style.","The original blue, cyan, and green colors stay in place.","The product wordmark changes to Averion Compass; Averion Software remains the parent."]
    for i,n in enumerate(notes):
        yy=118-i*12; c.setFillColor(HexColor(p["accent"])); c.circle(32,yy+2,1.6,fill=1,stroke=0); text(c,n,39,yy,5.1,p["ink"])
    swatch_x=490; swatch_y=116
    text(c,"PALETTE",swatch_x,132,4.5,p["muted"],"SegoeBold")
    keys=[("NAVY",p["navy"]),("SILVER",p["silver"]),("ACCENT",p["accent"]),("PAPER",p["paper"])]
    for i,(label,value) in enumerate(keys):
        xx=swatch_x+i*65
        c.setFillColor(HexColor(value)); c.setStrokeColor(HexColor(p["line"])); c.roundRect(xx,swatch_y,52,14,2,fill=1,stroke=1)
        text(c,label,xx,swatch_y-9,3.5,p["muted"],"SegoeBold")
    right_text(c,"Concept only - sample data is illustrative",PAGE_W-29,28,4.5,p["muted"])
    text(c,"AVERION COMPASS DESIGN REVIEW",29,28,4.5,p["muted"],"SegoeBold")
    c.showPage()

def main():
    c=canvas.Canvas(str(OUT),pagesize=landscape(letter),pageCompression=1)
    c.setTitle("Averion Compass - Design Samples")
    c.setAuthor("Averion Software")
    c.setSubject("Three website and application design directions based on the supplied Averion brand board")
    draw_page(c,1,"Gold-forward Averion","Keep the supplied silver, pearl, and navy identity; bring the gold accent forward.",GOLD,"new",True)
    draw_page(c,2,"Original blue, new design","Use the same redesign, logo treatment, layout, and font with the current blue palette.",BLUE,"new",False)
    draw_page(c,3,"Original design, renamed","Retain the CovePM-era page style and colors; replace the product name with Averion Compass.",LEGACY,"legacy",False)
    c.save()
    print(OUT)

if __name__=="__main__": main()
