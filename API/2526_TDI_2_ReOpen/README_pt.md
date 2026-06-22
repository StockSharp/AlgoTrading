# 2526 Estratégia TDI-2 ReOpen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma conversão em C# do consultor especialista do MetaTrader 5 **Exp_TDI-2_ReOpen**. Opera usando o indicador Trend Direction Index (TDI-2) e aplica a lógica original de re-entrada em posições. O port em C# usa a API de alto nível do StockSharp e mantém o comportamento central da versão MQL: reage a cruzamentos entre a linha de momentum TDI e a linha de índice TDI, escala em posições lucrativas após um avanço de preço configurável, e gerencia trades com stops protetores opcionais.

## Indicadores
- **Indicador TDI-2** – um indicador personalizado baseado em momentum implementado neste repositório. Constrói duas linhas:
  - *Linha direcional*: `Período × MomentumSuavizado`, onde o momentum é igual ao preço aplicado menos o preço `Período` barras atrás.
  - *Linha de índice*: `|Direcional| − (2 × Período × Suavizado(|Momentum|, 2×Período) − |Momentum|)`.
- O indicador suporta os seguintes métodos de suavização: média móvel Simples, Exponencial, Suavizada (RMA) e Linearmente Ponderada.
- As opções de preço aplicado suportadas replicam a implementação MQL original, incluindo as fórmulas TrendFollow e Demark.

## Lógica de trading
1. Em cada vela finalizada, a estratégia avalia os valores TDI-2 na barra especificada por **Signal Bar** (padrão: vela anterior fechada) e uma barra antes.
2. Quando a linha direcional estava acima da linha de índice e depois cruza abaixo:
   - Se **Allow Long Entries** estiver habilitado e não houver posição comprada ativa, a estratégia prepara uma nova entrada comprada.
   - Se existir uma posição vendida e **Allow Short Exits** estiver habilitado, fecha a posição vendida.
3. Quando a linha direcional estava abaixo da linha de índice e depois cruza acima:
   - Se **Allow Short Entries** estiver habilitado e não houver posição vendida ativa, a estratégia prepara uma nova entrada vendida.
   - Se existir uma posição comprada e **Allow Long Exits** estiver habilitado, fecha a posição comprada.
4. Lógica de re-entrada (escalonamento):
   - Enquanto mantém uma posição comprada, a estratégia rastreia o preço de execução do último trade comprado. Se o mercado se mover favoravelmente em **Re-entry Step (points)** e o número de trades comprados executados ainda estiver abaixo de **Max Entries**, abre uma ordem comprada adicional com o volume base.
   - A mesma lógica aplica-se a posições vendidas usando o preço de execução vendido mais recente.
5. Ao abrir uma posição enquanto existe uma posição contrária, a estratégia envia uma ordem a mercado combinada dimensionada tanto para fechar a exposição contrária quanto para estabelecer a nova posição com o volume base configurado.
6. Os níveis opcionais de stop-loss e take-profit são ativados através de `StartProtection` usando o multiplicador `PriceStep` do instrumento.

## Parâmetros
| Nome | Descrição | Valores padrão |
| --- | --- | --- |
| Money Management | Volume de ordem base. | 0.1 |
| Max Entries | Número máximo de entradas por direção (trade inicial + re-entradas). | 10 |
| Stop Loss (points) | Distância do stop-loss em pontos do instrumento. | 1000 |
| Take Profit (points) | Distância do take-profit em pontos do instrumento. | 2000 |
| Slippage (points) | Mantido por compatibilidade; não usado na implementação do StockSharp. | 10 |
| Re-entry Step (points) | Movimento mínimo favorável antes de escalar em uma posição existente. | 300 |
| Allow Long Entries / Allow Short Entries | Habilitar abertura de posições compradas/vendidas. | true |
| Allow Long Exits / Allow Short Exits | Habilitar fechamento de posições compradas/vendidas. | true |
| Candle Type | Série de velas usada para os cálculos. | Velas H4 |
| TDI Smoothing | Método de suavização para o indicador TDI-2. | MA Simples |
| TDI Period | Período de lookback do momentum. | 20 |
| TDI Phase | Reservado para compatibilidade com o input MQL (sem efeito nos modos de suavização suportados). | 15 |
| Applied Price | Fonte de preço usada pelo TDI-2. | Close |
| Signal Bar | Número de velas fechadas a olhar para trás ao avaliar cruzamentos. | 1 |

## Notas adicionais
- Apenas os métodos de suavização suportados pelos indicadores do StockSharp (SMA, EMA, SMMA, LWMA) são implementados. Outros modos MQL como JJMA ou T3 não estão disponíveis.
- O parâmetro **TDI Phase** é mantido por completude. Não influencia os métodos de suavização suportados e pode ser deixado no seu valor padrão.
- O parâmetro **Slippage (points)** é fornecido por paridade com o consultor especialista original, mas não é usado pela API de alto nível.
- Os contadores de re-entrada são reiniciados automaticamente quando a posição líquida retorna a zero.
