# Filtro Laguerre
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia opera os cruzamentos entre um filtro Laguerre e um filtro FIR curto construído como uma média móvel ponderada de preços medianos recentes.

- O filtro Laguerre suaviza o preço usando o parâmetro Gamma para reduzir o ruído.
- A linha FIR é uma média móvel ponderada de 4 períodos com pesos simétricos.
- Quando a linha FIR estava acima da linha Laguerre e cruza abaixo dela, a estratégia abre uma posição comprada.
- Quando a linha FIR estava abaixo e cruza acima da linha Laguerre, uma posição vendida é aberta.
- As posições opostas são fechadas quando a relação entre as linhas se inverte.
- Um stop-loss em porcentagem do preço de entrada protege cada operação.

Esta abordagem de reversão à média tenta capturar recuos quando o preço se desvia da curva Laguerre suavizada.
