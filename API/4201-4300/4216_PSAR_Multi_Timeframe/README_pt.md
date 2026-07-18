# PSAR Estratégia de vários prazos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia replica o consultor especialista MetaTrader **EA_PSar_002B**. Ele avalia valores Parabolic SAR em três períodos de tempo (M15, M30 e H1) enquanto gerencia posições em um fluxo de um minuto. A negociação é direcional: apenas uma posição líquida pode estar ativa por vez e novas negociações aparecem apenas quando a exposição anterior é estável. O especialista original foi projetado para EURUSD no gráfico M1 e a porta mantém o mesmo contexto.

## Lógica de negociação
1. **Parabolic SAR filtro de convergência** – os valores SAR mais recentes de M15, M30 e H1 devem estar dentro de 19 etapas de preço mínimo um do outro. Isso mantém as três curvas “apertadas” antes que um rompimento seja permitido.
2. **Entrada longa** – uma das seguintes sequências deve ocorrer:
   - Os valores M15, M30 e H1 SAR estão abaixo de seus respectivos mínimos atuais, o H1 anterior SAR estava acima da máxima H1 anterior e o novo H1 SAR cai abaixo da mínima H1 atual.
   - M15 e H1 SAR estão abaixo de seus mínimos atuais, enquanto o M30 anterior SAR estava acima da máxima anterior de M30 e o novo M30 SAR cai abaixo da mínima atual de M30.
   - M30 e H1 SAR estão abaixo de seus mínimos atuais, enquanto o M15 anterior SAR estava acima da máxima anterior de M15 e o novo M15 SAR cai abaixo da mínima atual de M15.
3. **Entrada curta** – condições de espelho da configuração longa com altos/baixos invertidos.
4. **Take Profit/Stop Loss** – os limites são expressos em pontos (incrementos mínimos de preço). Por padrão, a meta é igual a 999 pontos e o limite de proteção é igual a 399 pontos, que correspondem aos valores MQL após a normalização das cotações de 4/5 dígitos.
5. **Saída dinâmica** – enquanto uma posição está aberta o M30 SAR é monitorado.
   - As posições compradas fecham quando o SAR anterior estava abaixo da mínima M1 anterior, mas o SAR atual salta acima da máxima M1 atual.
   - As vendas fecham quando o SAR anterior estava acima da máxima M1 anterior, mas o SAR atual cai abaixo da mínima M1 atual.
   - Quando o M30 atual SAR ultrapassa o preço de entrada, o stop é rastreado até esse nível SAR.

## Gestão de dinheiro
`UseMoneyManagement` reproduz a mudança de gerenciamento de dinheiro do EA. Quando desativado, o parâmetro `FixedVolume` é usado. Quando ativado, a porcentagem solicitada de capital do portfólio é convertida para um tamanho de “lote” sintético usando a mesma fórmula da versão MQL (porcentagem de capital livre dividida por 100.000). O valor é alinhado a `Security.VolumeStep` e limitado aos limites do corretor (`VolumeMin`/`VolumeMax`).

## Parâmetros
- `BaseCandleType` – prazo usado para gerenciamento comercial (o padrão é M1).
- `FastSarCandleType`, `MediumSarCandleType`, `SlowSarCandleType` – prazos para os filtros SAR (padrões: 15m, 30m, 60m).
- `EnableParabolicFilter` – espelha o sinalizador `sar2` de MQL; desligá-lo interrompe completamente a negociação.
- `TakeProfitPoints`, `StopLossPoints` – compensações em pontos (incrementos mínimos de preço). O tamanho do pip é derivado de `Security.PriceStep` e `Security.Decimals` para lidar corretamente com cotações forex de 3/5 dígitos.
- `UseMoneyManagement`, `PercentMoneyManagement`, `FixedVolume` – controles de volume descritos acima.

## Notas de conversão
- Somente o StockSharp API de alto nível é usado. Todas as séries de preços são assinadas por meio de `SubscribeCandles().Bind(...)` e os dados do indicador são recebidos por meio de ligações em vez de buffers manuais.
- As ordens de proteção são implementadas por saídas explícitas do mercado, exatamente como o script original chamado `OrderClose`.
- O coeficiente de dígito do corretor de MQL é substituído pela detecção automática do tamanho do pip (`PriceStep` × 10 para instrumentos de 3/5 dígitos).
- O EA proibiu a negociação de símbolos não EURUSD ou gráficos não M1 por meio da impressão de mensagens. Em StockSharp os logs de estratégia permanecem silenciosos, mas o comportamento está documentado aqui.

## Dicas de uso
1. Anexe a estratégia ao EURUSD com velas de um minuto para a assinatura base. Os prazos dos indicadores ainda podem ser alterados se a experimentação for desejada.
2. Certifique-se de que os metadados de segurança exponham `PriceStep`/`Decimals`. Sem eles, as distâncias de parada e alvo voltam para um tamanho de unidade de 1.
3. Mantenha `EnableParabolicFilter` ativado; é equivalente à chave mestre do EA. Desative-o apenas quando desejar intencionalmente que a estratégia permaneça ociosa.
