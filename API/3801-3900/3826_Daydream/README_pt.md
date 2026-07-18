# Estratégia de sonhar acordado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia Daydream** é uma conversão direta do consultor especialista MQL4 *Daydream by Cothol*. O robô original negocia o gráfico USD/JPY H1 observando os rompimentos de um canal de preços recente e, em seguida, gerenciando as negociações com um lucro final virtual. Esta porta StockSharp mantém a mesma lógica central enquanto usa o API de alto nível: Donchian Os canais entregam os níveis de breakout, os pedidos são feitos por meio de `BuyMarket` / `SellMarket`, e toda a lógica final é tratada dentro da estratégia sem colocar ordens de lucro reais na bolsa.

Características principais:

- Sistema de breakout de posição única que só muda de direção depois que uma vela fecha fora dos extremos do canal anterior.
- Take Profit virtual medido em pips que aumenta com o preço para bloquear lucros e fechar negociações quando alcançado.
- Limitação de entrada para que apenas uma ação de negociação (abertura/fechamento) possa acontecer por vela, espelhando a restrição MQL4 `LastOrderTime`.

## Lógica de negociação

1. Construa um canal Donchian com `ChannelPeriod` velas concluídas e armazene os níveis superiores/inferiores anteriores.
2. Quando uma vela fecha **abaixo** da faixa inferior anterior:
   - Fechar uma posição curta existente.
   - Na próxima vela, abra uma nova posição longa com `OrderVolume` e defina o nível de lucro virtual para `close + TakeProfitPips * pipSize`.
3. Quando uma vela fecha **acima** da faixa superior anterior:
   - Fechar uma posição longa existente.
   - Na próxima vela, abra uma nova posição curta e defina o take-profit virtual em `close - TakeProfitPips * pipSize`.
4. Enquanto uma posição estiver ativa, reduza o preço de lucro virtual de cada barra. Se o preço atingir esse nível em uma vela subsequente, saia da negociação.

O tamanho do pip é derivado da segurança `PriceStep`. Para pares JPY, isso converte um passo de 0,001 em um incremento de 0,01 pip, correspondendo ao comportamento MQL.

## Parâmetros

| Nome | Descrição | Padrão | Notas |
|------|-------------|---------|-------|
| `OrderVolume` | Volume utilizado para cada nova entrada no mercado. | `1` | Corresponde à entrada `Lots` do especialista MQL. |
| `ChannelPeriod` | Número de velas concluídas no canal Donchian. | `25` | Espelha `ChannelPeriod` em MQL. |
| `Slippage` | Deslizamento permitido em pontos. | `3` | Armazenado para integridade; as ordens de mercado ignoram-no. |
| `TakeProfitPips` | Distância do lucro virtual em pips. | `15` | Move-se com o preço enquanto a posição está aberta. |
| `CandleType` | Prazo usado para construir o canal Donchian. | `1 hour` | Prazo padrão da estratégia original. |

## Diagrama de Fluxo de Trabalho

```
Vela fecha
│
├─► Atualizar canal Donchian (bandas anteriores)
│
├─► Rompimento abaixo do mínimo anterior? ──► Fechar curto → agendar a próxima barra longa
│
├─► Rompimento acima da máxima anterior? ─► Fechar longo → agendar próximo bar curto
│
└─► Trilha o lucro virtual na direção da posição aberta
└─► O preço atingiu a meta virtual? → Fechar posição
```

## Notas de uso

- Anexe a estratégia a qualquer título com streaming de velas. As configurações padrão correspondem à recomendação original de USD/JPY H1.
- Existe apenas uma posição por vez. A estratégia evita a abertura e o fechamento de negociações dentro da mesma vela para replicar a lógica MQL4.
- O take-profit é virtual: a saída ocorre por meio de uma ordem de mercado quando o nível calculado é ultrapassado. Nenhuma ordem TP real é enviada ao corretor.
- Ajuste `CandleType` para ser executado em intervalos de tempo diferentes. Períodos mais elevados requerem dados históricos suficientes para aquecer o canal Donchian.

## Diferenças da versão MQL4

- Usa o indicador StockSharp `DonchianChannels` em vez de verificar manualmente os altos e baixos.
- O lucro final e a limitação de ação são preservados, mas a execução usa StockSharp ordens de mercado sem depender do gerenciamento de tickets MT4.
- O parâmetro `Slippage` é mantido para paridade, embora a execução de mercado em StockSharp não aplique slippage da mesma forma que MT4.

## Arquivos

- `CS/DaydreamStrategy.cs` – implementação de estratégia em C#.
- Versão Python: ainda não implementada.
