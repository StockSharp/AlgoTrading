# Estratégia Exp Oracle
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma portação em C# do consultor especialista MetaTrader **Exp_Oracle**. Ela depende de um indicador personalizado *Oracle* que mistura o Índice de Força Relativa (RSI) e o Índice de Canal de Commodities (CCI) para prever a direção do mercado vários bares à frente. O indicador gera duas linhas:

- **Linha Oracle** – combinação bruta dos extremos de CCI e RSI.
- **Linha de sinal** – média móvel suavizada da linha Oracle.

A estratégia fornece três modos de negociação para interpretar essas linhas:

1. **Breakdown** – abre posições quando a linha de sinal cruza o nível zero.
2. **Twist** – reage a pontos de inflexão locais da linha de sinal.
3. **Disposition** – opera nos cruzamentos entre a linha de sinal e a linha Oracle.

## Parâmetros

- `OraclePeriod` – período para os cálculos de RSI e CCI.
- `Smooth` – número de barras usadas para suavizar a linha de sinal.
- `Mode` – algoritmo usado para gerar sinais de negociação (`Breakdown`, `Twist` ou `Disposition`).
- `CandleType` – período das velas recebidas.
- `AllowBuy` – habilita entradas compradas.
- `AllowSell` – habilita entradas vendidas.
- `Volume` – volume da estratégia herdado da classe base `Strategy`.

## Regras de entrada e saída

### Breakdown
- **Comprar** quando a linha de sinal cruza acima de zero.
- **Vender** quando a linha de sinal cruza abaixo de zero.

### Twist
- **Comprar** quando a linha de sinal vira para cima após uma queda.
- **Vender** quando a linha de sinal vira para baixo após uma subida.

### Disposition
- **Comprar** quando a linha de sinal cruza acima da linha Oracle.
- **Vender** quando a linha de sinal cruza abaixo da linha Oracle.

As posições existentes são fechadas e revertidas quando um sinal oposto aparece. A estratégia usa ordens de mercado por simplicidade.

## Lógica do indicador

Para cada barra:
1. Calcular RSI e CCI com o `OraclePeriod` especificado.
2. Construir quatro valores de divergência usando diferenças entre valores recentes de CCI e RSI.
3. A linha Oracle é a soma da divergência máxima e mínima.
4. A linha de sinal é a média móvel simples da linha Oracle ao longo de `Smooth` barras.

Esta abordagem tenta prever o movimento de preço de curto prazo combinando momentum (RSI) e canal (CCI).

## Notas

- A estratégia opera somente em velas concluídas.
- Stops de proteção não estão implementados; use controles de risco externos se necessário.
