# Estratégia de Vela XDPO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma conversão do expert MQL5 original **Exp_XDPOCandle**. Ela constrói velas sintéticas aplicando duas médias móveis exponenciais consecutivas aos preços de abertura e fechamento. A cor da vela resultante (altista, baixista ou neutra) orienta as decisões de trading.

## Lógica da estratégia

1. Cada vela de mercado recebida é suavizada duas vezes:
   - O primeiro suavizamento usa uma EMA de comprimento `FastLength`.
   - O segundo suavizamento aplica outra EMA de comprimento `SlowLength` ao resultado do primeiro.
2. Se o fechamento suavizado está acima da abertura suavizada, a vela é considerada *altista*.
3. Se o fechamento suavizado está abaixo da abertura suavizada, a vela é considerada *baixista*.
4. A estratégia abre uma posição comprada quando uma vela altista aparece após uma não altista. Abre uma posição vendida quando uma vela baixista aparece após uma não baixista.
5. Posições opostas existentes são fechadas automaticamente revertendo através de ordens de mercado.

## Parâmetros

| Nome | Descrição |
|------|-----------|
| `FastLength` | Comprimento da primeira EMA aplicada aos preços. |
| `SlowLength` | Comprimento da segunda EMA aplicada ao resultado da primeira EMA. |
| `CandleType` | O período e tipo de velas usadas para o cálculo. |

## Uso

1. Anexe a estratégia a um instrumento dentro do ambiente StockSharp.
2. Configure os parâmetros se necessário. Os valores padrão estão ajustados para corresponder às configurações originais do expert.
3. Inicie a estratégia. Ela irá se inscrever no tipo de vela especificado e operar nas mudanças de cor das velas suavizadas.

## Notas

- O gerenciamento de risco é tratado por `StartProtection()` com configurações padrão. Ajuste `Volume` e parâmetros de proteção externamente conforme necessário.
- Este repositório atualmente contém apenas a versão em C#; o port em Python não está disponível.
