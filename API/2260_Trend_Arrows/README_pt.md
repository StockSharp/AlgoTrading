# Estratégia de Setas de Tendência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera rompimentos quando o preço de fechamento se move além de extremos recentes.
Ela calcula os preços de fechamento mais altos e mais baixos durante um período configurável.
Uma nova tendência de alta é detectada quando o fechamento supera a máxima recente,
enquanto uma tendência de baixa começa quando o fechamento cai abaixo da mínima recente.

Quando uma nova tendência de alta é detectada, posições vendidas existentes podem ser fechadas e posições compradas opcionais abertas.
Ao contrário, uma nova tendência de baixa permite fechar posições compradas e opcionalmente abrir vendidas.
A estratégia processa apenas velas completadas e usa a API de alto nível do StockSharp.

## Parâmetros
- **Period** – número de barras para determinar os extremos recentes.
- **Candle Type** – período das velas.
- **Open Long** – permitir a abertura de posições compradas.
- **Open Short** – permitir a abertura de posições vendidas.
- **Close Long** – permitir o fechamento de posições compradas.
- **Close Short** – permitir o fechamento de posições vendidas.

## Lógica
1. Inscrever-se nos dados de velas do período selecionado.
2. Rastrear os fechamentos mais altos e mais baixos ao longo do período usando os indicadores `Highest` e `Lowest`.
3. Quando o preço rompe acima do fechamento mais alto, sinalizar tendência de alta; quando abaixo do fechamento mais baixo, sinalizar tendência de baixa.
4. Entrar ou sair de posições de acordo com a nova tendência e as opções habilitadas.
