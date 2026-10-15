# Estratégia TCPivotLimit do GPF
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia GPF TCPivotLimit** recria o MetaTrader 4 consultor especialista `gpfTCPivotLimit.mq4` dentro da estrutura StockSharp. O sistema negocia em **velas horárias** e reage a reversões em torno dos clássicos **níveis de pivô diários**. A cada novo dia de negociação, a estratégia calcula o pivô, três níveis de resistência (R1–R3) e três níveis de suporte (S1–S3) da máxima, mínima e fechamento do dia anterior. Assim que o dia seguinte começa, ele avalia as duas últimas velas horárias concluídas para decidir se o preço rejeitou uma zona de resistência ou suporte e abre uma ordem de mercado na direção oposta.

## Lógica de negociação

1. **Cálculo de pivô** – quando uma nova sessão diária começa, a estratégia armazena a máxima, a mínima e o fechamento do dia anterior e, em seguida, calcula:
   - `Pivot = (High + Low + Close) / 3`
   - `R1 = 2 × Pivot − Low`, `S1 = 2 × Pivot − High`
   - `R2 = Pivot + (High − Low)`, `S2 = Pivot − (High − Low)`
   - `R3 = High + 2 × (Pivot − Low)`, `S3 = Low − 2 × (High − Pivot)`
2. **Confirmação de entrada** – com o novo dia em andamento as duas últimas velas horárias fechadas (`t-2` e `t-1`) são inspecionadas.
   - Uma **venda** será aberta se a vela `t-2` for sondada acima da resistência selecionada (alta acima ou perto do nível), aberta abaixo dela e a vela `t-1` fechar abaixo do nível.
   - Uma **longa** será aberta se a vela `t-2` cair abaixo do suporte selecionado (baixo abaixo ou fechar no nível), abrir acima dele e a vela `t-1` fechar novamente acima do nível.
3. **Predefinições de metas** – o consultor especialista original expõe cinco layouts de lucro/stop. A tabela abaixo mostra o mapeamento exato preservado nesta porta.

| `TargetMode` | Gatilho longo | Parada longa | Alvo longo | Gatilho curto | Parada curta | Alvo curto |
|-------------:|--------------|-----------|-------------|---------------|------------|--------------|
| 1 | `S1` | `S2` | `R1` | `R1` | `R2` | `S1` |
| 2 | `S1` | `S2` | `R2` | `R1` | `R2` | `S2` |
| 3 | `S2` | `S3` | `R1` | `R2` | `R3` | `S1` |
| 4 | `S2` | `S3` | `R2` | `R2` | `R3` | `S2` |
| 5 | `S2` | `S3` | `R3` | `R2` | `R3` | `S3` |

4. **Gerenciamento de risco** – verificações protetoras de stop-loss e take-profit são executadas em cada vela concluída. A lógica opcional de trailing stop emula o comportamento do MT4: quando o lucro não realizado excede a distância configurada, o stop é movido em favor da negociação. Uma saída opcional no final do dia nivela a posição às 23h, horário da plataforma.

5. **Adaptação de volume** – a entrada MetaTrader `isFloatLots` é espelhada pelo botão de alternância `UseDynamicVolume`. Quando ativado, o tamanho da posição é reduzido após negociações consecutivas com perdas, usando as entradas `DrawdownFactor` e `RiskPercentage`.

## Parâmetros

| Nome | Descrição | Padrão |
|------|-------------|---------|
| `BaseVolume` | Volume base enviado com cada ordem de mercado antes dos ajustes de risco. | `1` |
| `UseDynamicVolume` | Reduz o tamanho da negociação após mais de uma perda consecutiva. | `false` |
| `RiskPercentage` | Razão de risco por negociação de referência usada para dimensionar o volume base (MetaTrader `MaxR`). | `0.02` |
| `DrawdownFactor` | Divisor aplicado ao diminuir o volume após uma seqüência de derrotas (MetaTrader `DcF`). | `3` |
| `TargetMode` | Seleciona a combinação de resistência/suporte listada acima (MetaTrader `TgtProfit`). | `1` |
| `TrailingPoints` | Distância de parada móvel expressa em pontos do instrumento. Defina como `0` para desativar. | `30` |
| `CloseAtSessionEnd` | Quando `true` todas as posições são fechadas no fechamento da vela das 23:00. | `false` |
| `LogSignals` | Imprime valores pivô, entradas e saídas no log de estratégia. | `false` |
| `CandleType` | Tipo de dados de vela usado para análise (o padrão é velas de 1 hora). | `TimeFrameCandleMessage(1h)` |

## Notas

- A estratégia emite **ordens de mercado** assim como o EA original e não coloca ordens pendentes.
- Os eventos stop-loss e take-profit são executados com saídas de mercado para permanecerem compatíveis com todos os conectores StockSharp.
- As distâncias finais dependem do instrumento `PriceStep`. Se a etapa estiver faltando, o mecanismo de rastreamento será automaticamente desativado.
- O sinalizador de notificação por e-mail da versão MT4 é representado por `LogSignals`, produzindo mensagens de log em vez de e-mails.
