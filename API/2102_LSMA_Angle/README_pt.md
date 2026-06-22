# Estratégia de Ângulo LSMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa o ângulo da Média Móvel de Mínimos Quadrados (LSMA) para detectar a direção da tendência. O ângulo é aproximado pela diferença entre dois valores de LSMA separados por um número configurável de barras.

- **Entrada comprada**: o ângulo LSMA sobe acima do limiar positivo.
- **Saída comprada**: o ângulo retorna abaixo do limiar positivo.
- **Entrada vendida**: o ângulo LSMA cai abaixo do limiar negativo.
- **Saída vendida**: o ângulo retorna acima do limiar negativo.

## Parâmetros
- `LSMA Period`: comprimento para o cálculo do LSMA.
- `Angle Threshold`: valor absoluto que define a zona neutra ao redor de zero.
- `Start Shift`: barra mais antiga usada para calcular o ângulo.
- `End Shift`: barra mais recente usada para calcular o ângulo.
- `Candle Type`: tipo de dados de vela para o cálculo.

## Notas
- Os valores do ângulo são escalados para pontos dependendo do instrumento (1000 para pares JPY, caso contrário 100000).
- Funciona apenas em velas completadas.
