# Estratégia de Cruzamento KPrmSt
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia KPrmSt Cross é uma portagem do expert do MetaTrader 5 `exp_kprmst.mq5`. Ela usa um oscilador semelhante ao Stochastic conhecido como KPrmSt para capturar reversões quando a linha principal do oscilador cruza a linha de sinal.

A estratégia assina velas de um período configurável e calcula o indicador `Stochastic` (usado como aproximação do KPrmSt). Quando a linha %K cruza abaixo da linha %D, abre uma posição comprada; quando %K cruza acima de %D, abre uma posição vendida. As posições existentes são revertidas conforme necessário.

## Parâmetros
- `Candle Type` – período das velas usadas para os cálculos.
- `K Period` – número de barras para calcular a linha principal.
- `D Period` – período para suavização da linha de sinal.
- `Slowing` – suavização adicional aplicada a %K.
- `Stop Loss` – perda protetora em unidades de preço. Definir como 0 para desativar.
- `Take Profit` – lucro alvo em unidades de preço. Definir como 0 para desativar.

## Lógica de trading
1. A estratégia ouve apenas velas finalizadas.
2. Os valores do oscilador Stochastic são armazenados para detectar cruzamentos.
3. Quando %K cai abaixo de %D após estar acima dela, uma posição comprada é aberta ou a vendida é fechada.
4. Quando %K sobe acima de %D após estar abaixo dela, uma posição vendida é aberta ou a comprada é fechada.
5. Níveis opcionais de stop loss e take profit fecham a posição quando atingidos.

## Observações
- O indicador KPrmSt do expert original é aproximado pelo indicador `Stochastic` do StockSharp.
- As opções de gestão de capital do script original não estão implementadas.
- A estratégia requer feed de dados de mercado e roteamento de ordens suportado pelo StockSharp.
