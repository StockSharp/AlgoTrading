# Estratégia de Retração na Nuvem Ichimoku
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é um port do StockSharp do expert do MetaTrader "ichimok2005". Ela procura retrações para dentro da nuvem Ichimoku e opera na direção da inclinação predominante do kumo. Os sinais são avaliados apenas em velas concluídas.

## Visão geral

- Funciona em qualquer instrumento e período que forneça dados de velas.
- Usa as configurações padrão do Ichimoku (9/26/52) por padrão, mas são totalmente configuráveis.
- Opera tanto comprado quanto vendido. O tamanho da posição é definido pela propriedade `Volume` da estratégia.
- Stop-loss e take-profit opcionais podem ser configurados em unidades de preço absolutas.

## Indicadores e parâmetros

- **Ichimoku**: os comprimentos de `Tenkan`, `Kijun` e `Senkou Span B` são expostos como parâmetros.
- **Tipo de vela**: escolha qualquer tipo de vela agregada suportada pela conexão (padrão: período de 1 hora).
- **Stop Loss Offset**: distância opcional abaixo/acima do preço de entrada que força uma saída. Defina como `0` para desabilitar.
- **Take Profit Offset**: distância opcional do alvo de lucro a partir do preço de entrada. Defina como `0` para desabilitar.

## Critérios de entrada

### Configuração comprada

1. `Senkou Span A` está acima de `Senkou Span B`, sinalizando uma nuvem de alta.
2. A vela concluída atual é de alta (`Close > Open`).
3. A vela fecha dentro da nuvem (`Close` está entre os dois spans).
4. Quando todas as condições se alinham e a estratégia está plana ou vendida, envia uma ordem de compra a mercado dimensionada para fechar qualquer exposição vendida e abrir uma nova posição comprada.

### Configuração vendida

1. `Senkou Span B` está acima de `Senkou Span A`, sinalizando uma nuvem de baixa.
2. A vela concluída atual é de baixa (`Open > Close`).
3. A vela fecha dentro da nuvem (`Close` está entre os dois spans).
4. Quando as condições se alinham e a estratégia está plana ou comprada, envia uma ordem de venda a mercado dimensionada para fechar qualquer exposição comprada e abrir uma nova posição vendida.

## Critérios de saída

- Sinais opostos revertem automaticamente a posição combinando o fechamento e a nova entrada em uma única ordem a mercado.
- Quando habilitado, `Stop Loss Offset` sai em `EntryPrice - Offset` para comprados e `EntryPrice + Offset` para vendidos, usando o preço de fechamento da vela.
- Quando habilitado, `Take Profit Offset` sai em `EntryPrice + Offset` para comprados e `EntryPrice - Offset` para vendidos.
- O fechamento manual (encerramento da estratégia) também redefine o rastreador interno de preço de entrada.

## Notas de gestão de risco

- Os offsets são expressos em unidades de preço absolutas. Converta distâncias em pips ou ticks para preço antes de configurá-los.
- Como a estratégia usa preços de fechamento de velas para verificações de risco, considere offsets mais estreitos para períodos menores.
- Não há saídas parciais ou trailing implementados; a estratégia sempre encerra a posição inteira.

## Detalhes adicionais de implementação

- A estratégia assina velas através da API de alto nível e vincula o indicador Ichimoku com `BindEx`.
- Apenas velas concluídas acionam lógica; atualizações intermediárias são ignoradas.
- Uma área de gráfico é criada automaticamente (quando disponível) para exibir preço, a nuvem Ichimoku e as operações executadas.
- `ManageRisk` é executado antes de buscar novas entradas para que as saídas de proteção tenham prioridade.
