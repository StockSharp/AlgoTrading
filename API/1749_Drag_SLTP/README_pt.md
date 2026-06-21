# Estratégia de Gestão Drag SL/TP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia coloca automaticamente ordens de stop-loss e take-profit a uma distância fixa do preço da operação executada. É útil quando posições manuais devem ser protegidas imediatamente após a entrada.

## Parâmetros

- **Auto Set SL** (`bool`): habilitar a colocação automática de stop-loss.
- **SL Points** (`decimal`): distância do stop-loss em passos de preço.
- **Auto Set TP** (`bool`): habilitar a colocação automática de take-profit.
- **TP Points** (`decimal`): distância do take-profit em passos de preço.

## Comportamento

Quando a estratégia é iniciada, ela chama `StartProtection` com as distâncias selecionadas. Qualquer posição aberta enquanto a estratégia estiver em execução receberá imediatamente as ordens de proteção correspondentes. As distâncias são medidas em passos de preço (`Security.PriceStep`).

A própria estratégia não gera sinais de trading; ela simplesmente gerencia ordens de proteção para posições abertas manualmente ou por outras estratégias.

## Notas

- Projetada para uso com a API de alto nível.
- Apenas o estado de candle finalizado deve acionar ações de trading em versões estendidas.
- A função de arrastar gráfico do script MQL original não está implementada.
