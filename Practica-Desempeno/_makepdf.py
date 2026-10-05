# -*- coding: utf-8 -*-
from fpdf import FPDF

ACCENT = (0, 90, 160)
GREY = (90, 90, 90)

steps = [
    ('[ Apretar REC en la pantalla de login. Mandar Ctrl+Alt+Supr\n  (menu Entrada -> Teclado -> Insertar Ctrl-Alt-Supr) ]',
     'Hola, mi nombre es Sammy Garcia y voy a presentar mi laboratorio de redes en Windows Server 2022.'),
    ('[ Mostrar el banner que aparece con tu nombre ]',
     'Como ven, al iniciar sesion aparece el banner de mi GPO con mi nombre. Acepto e ingreso.'),
    ('[ Escribir la contrasena (Samuel030405) e iniciar sesion. Abrir PowerShell y escribir:  hostname  /  whoami ]',
     'Confirmo mi identidad: el equipo se llama SRV-SammyGarcia y estoy logueado en mi dominio.'),
    ('[ Escribir:  Get-ADDomain ]',
     'Tengo Active Directory configurado. Mi dominio es sammygarcia.local y este equipo es el controlador de dominio.'),
    ('[ Escribir:  nslookup sammygarcia.local ]',
     'El servicio DNS resuelve correctamente: el dominio apunta a la IP 192.168.10.10.'),
    ('[ Abrir Edge en  http://localhost ]',
     'Como segundo servicio tengo IIS, el servidor web. Aca esta mi pagina publicada con mi nombre.'),
    ('[ En Edge escribir  ftp://localhost  y hacer clic en bienvenida.txt ]',
     'Tambien configure un servidor FTP. Me conecto con el usuario del dominio ftpalumno y accedo a mi archivo.'),
    ("[ Server Manager -> Tools -> Group Policy Management -> pestana 'Linked Group Policy Objects' ]",
     'Aca esta mi GPO, GPO-Banner-SammyGarcia, enlazada a mi dominio y habilitada.'),
    ('[ Volver a PowerShell y escribir:  C:\\Users\\Administrator\\Desktop\\demo-video.ps1 ]',
     'Por ultimo, este resumen confirma que Active Directory, DNS, IIS, FTP y la GPO estan operativos, con conectividad por ping. Soy Sammy Garcia, muchas gracias.'),
    ('[ Cortar la grabacion ]', ''),
]

pdf = FPDF(format="A4")
pdf.set_auto_page_break(auto=True, margin=15)
pdf.add_page()
pdf.set_margins(18, 16, 18)

pdf.set_font("Helvetica", "B", 16)
pdf.set_text_color(*ACCENT)
pdf.cell(0, 9, "Libreto del Video - Desempeno Redes", new_x="LMARGIN", new_y="NEXT")
pdf.set_font("Helvetica", "B", 12)
pdf.set_text_color(0, 0, 0)
pdf.cell(0, 7, "Alumno: Sammy Garcia", new_x="LMARGIN", new_y="NEXT")
pdf.set_font("Helvetica", "I", 9)
pdf.set_text_color(*GREY)
pdf.cell(0, 6, "Maximo 2 minutos  -  Una sola toma  -  Sin editar  -  Formato .mp4", new_x="LMARGIN", new_y="NEXT")
pdf.cell(0, 6, "Hace lo que esta entre [corchetes] y lee en voz alta lo que esta entre comillas.", new_x="LMARGIN", new_y="NEXT")
pdf.ln(3)
pdf.set_draw_color(*ACCENT)
pdf.set_line_width(0.5)
y = pdf.get_y()
pdf.line(18, y, 192, y)
pdf.ln(4)

for i, (accion, dice) in enumerate(steps, 1):
    pdf.set_font("Helvetica", "B", 10)
    pdf.set_text_color(*ACCENT)
    pdf.multi_cell(pdf.epw, 5.5, f"{i}.  {accion}")
    if dice:
        pdf.set_font("Helvetica", "", 11)
        pdf.set_text_color(20, 20, 20)
        pdf.multi_cell(pdf.epw, 6, '"' + dice + '"')
    pdf.ln(2.5)

pdf.ln(2)
pdf.set_draw_color(180, 180, 180)
y = pdf.get_y()
pdf.line(18, y, 192, y)
pdf.ln(3)
pdf.set_font("Helvetica", "B", 10)
pdf.set_text_color(*ACCENT)
pdf.cell(0, 6, "Datos utiles", new_x="LMARGIN", new_y="NEXT")
pdf.set_font("Helvetica", "", 10)
pdf.set_text_color(0, 0, 0)
for line in [
    "Login Windows :  Administrator  /  Samuel030405",
    "FTP           :  ftpalumno  /  Ftp.2024Segura",
    "Dominio       :  sammygarcia.local   (IP 192.168.10.10)",
]:
    pdf.cell(0, 5.5, line, new_x="LMARGIN", new_y="NEXT")

pdf.output(r"C:\Users\Usuario\KawaSys\Practica-Desempeno\LIBRETO-VIDEO.pdf")
print("PDF generado OK")
