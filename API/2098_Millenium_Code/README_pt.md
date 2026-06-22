# Estratégia Millenium Code
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Millenium Code** é um sistema posicional que abre no máximo uma operação por dia. A direção é determinada por um cruzamento de médias móveis filtrado por máximas e mínimas recentes. As operações são colocadas em um horário definido pelo usuário e são fechadas por tempo, stop loss, take profit ou duração máxima.

## Lógica de Negociação

1. No horário de abertura especificado, a estratégia verifica se a negociação é permitida para o dia da semana atual.
2. Médias móveis simples rápida e lenta são comparadas. Se a MA rápida cruza acima da MA lenta e o preço confirma o rompimento, uma posição comprada é aberta. As condições opostas abrem uma posição vendida.
3. Apenas uma operação por dia é permitida. Sinais subsequentes são ignorados até o próximo dia de negociação.
4. As posições são fechadas quando:
   - O nível de stop loss ou take profit é atingido.
   - O horário de fechamento configurado ocorre.
   - A duração máxima da operação é excedida.

## Parâmetros

- **Candle Type** – período das velas de entrada.
- **Fast MA** – período da média móvel rápida.
- **Slow MA** – período da média móvel lenta.
- **HighLow Bars** – número de velas usadas para buscar máximas e mínimas recentes.
- **Reverse** – inverter sinais de compra/venda.
- **Stop Loss** – distância ao stop loss em passos de preço.
- **Take Profit** – distância ao take profit em passos de preço.
- **Open Hour/Minute** – horário para começar a procurar entradas (-1 desabilita).
- **Close Hour/Minute** – horário para fechar posições (-1 desabilita).
- **Duration** – vida máxima da operação em horas (0 desabilita).
- **Sunday ... Friday** – habilitar negociação para cada dia da semana.

## Notas

Esta estratégia usa apenas recursos de API de alto nível e evita acessar o histórico do indicador diretamente. Destina-se como exemplo educacional e não como conselho de investimento.
