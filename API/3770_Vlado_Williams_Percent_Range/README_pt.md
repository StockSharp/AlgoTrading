# Vlado Williams %R Estratégia de Limite
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Vlado Williams %R Threshold Strategy** é uma conversão direta do MetaTrader 4 consultor especialista `Vlado_www_forex-instruments_info.mq4`. O robô original negocia um único oscilador Williams %R e inverte sua exposição de mercado sempre que o indicador cruza um nível definido pelo usuário. Esta porta StockSharp reproduz o mesmo comportamento de mudança de regime enquanto expõe cada valor ajustável como um parâmetro de estratégia para otimização e controle de UI.

### Conceitos-chave
- Negocia a direção do oscilador Williams %R em relação a um limite (padrão `-50`).
- Mantém no máximo uma posição de mercado por vez e reverte somente após o fechamento da negociação anterior.
- Dimensionamento opcional de posição baseado em risco que imita a MetaTrader fórmula de gerenciamento de dinheiro `AccountFreeMargin * MaximumRisk / price`.
- Funciona com qualquer período de vela através do parâmetro `CandleType` (barras padrão de 15 minutos).

## Lógica de negociação
1. Assine o fluxo de vela configurado e calcule um Williams %R de comprimento `WprLength` (padrão 100).
2. Quando Williams %R sobe **acima** `WprLevel`, a estratégia marca um viés de alta:
   - Se nenhuma posição estiver aberta e a negociação anterior não tiver sido longa, envie uma ordem de **compra** a mercado.
   - Se existir uma posição curta, ela será fechada imediatamente; novas posições compradas são consideradas na próxima vela após a posição ser plana.
3. Quando Williams %R cai **abaixo** de `WprLevel`, a tendência muda para baixa:
   - Se nenhuma posição estiver aberta e a negociação anterior não tiver sido vendida, envie uma ordem de **venda** a mercado.
   - Se existir uma posição longa, ela será achatada imediatamente.
4. O tamanho da posição é determinado por `CalculateOrderVolume`:
   - Quando `UseRiskMoneyManagement` é **verdade**, a estratégia estima o volume negociável a partir do valor atual do portfólio: `Portfolio.CurrentValue × MaximumRiskPercent ÷ 100 ÷ ClosePrice`.
   - Caso contrário, a base `Strategy.Volume` será usada.
   - Os lotes resultantes são alinhados ao instrumento `VolumeStep` e limitados por `MinVolume` / `MaxVolume` se esses limites estiverem disponíveis.

A estratégia evita intencionalmente abrir uma posição de reversão na mesma vela que acionou a saída, correspondendo ao fluxo EA original (`CheckForClose` é executado antes de `CheckForOpen`).

## Notas de conversão
- Os padrões de gerenciamento de dinheiro seguem o script MT4: `MaximumRiskPercent` começa em `10`, correspondendo à constante `MaximumRisk = 10` original que visava aproximadamente um minilote por negociação.
- O parâmetro `shift` de MetaTrader (mudança do indicador) é sempre zero no arquivo de origem; portanto, foi omitido.
- Argumentos de cores MT4 (por exemplo, `Red`, `Blue`) não têm equivalente StockSharp e são ignorados.
- As entradas de deslizamento não são necessárias porque StockSharp ordens de mercado já usam o melhor preço atual.

## Parâmetros
| Parâmetro | Tipo | Padrão | Descrição |
|-----------|------|---------|-------------|
| `CandleType` | `DataType` | Período de 15 minutos | Prazo para cálculo de sinal e acionamentos de ordem. |
| `WprLength` | `int` | 100 | Período de lookback do oscilador Williams %R. |
| `WprLevel` | `decimal` | `-50` | Limiar que separa os regimes de alta e de baixa. |
| `UseRiskMoneyManagement` | `bool` | `false` | Alterna o dimensionamento da posição com base no risco. |
| `MaximumRiskPercent` | `decimal` | `10` | Porcentagem do patrimônio do portfólio implantado por negociação quando o gerenciamento de risco está ativado. |

> **Dica:** Combine a estratégia com `StartProtection()` ou controles de risco externos se precisar de tratamento automático de stop-loss. O EA original também dependia de supervisão manual e não definia paradas bruscas.

## Diretrizes de uso
1. Anexe a estratégia a um título que exponha `PriceStep`, `StepPrice`, `VolumeStep` e limites de volume precisos para que o auxiliar de dimensionamento de posição possa normalizar os pedidos corretamente.
2. Defina `Volume` para o tamanho do lote substituto desejado. Ele será usado sempre que o patrimônio do portfólio estiver indisponível ou `UseRiskMoneyManagement` estiver desativado.
3. Otimize `WprLevel` e `WprLength` para adaptar o sistema a diferentes mercados. Níveis estreitos (por exemplo, `-20` / `-80`) tornam a estratégia mais seletiva, enquanto limites amplos (`-50`) garantem que ela seja quase sempre investida.
4. A estratégia segue tendências: será revertida frequentemente em condições variadas. Considere combiná-lo com filtros, como verificações de tendências de prazos mais elevados ou limites de volatilidade, quando necessário.

## Diferenças vs. versão MetaTrader
- Usa assinaturas de velas e ligações de indicadores do StockSharp API de alto nível; não há loop de pedido manual ou verificação de histórico.
- O dimensionamento do risco depende de `Portfolio.CurrentValue`. Quando falta a avaliação da conta, a lógica volta para o `Volume` estático, correspondendo ao comportamento MT4 onde `mm=0` forçou um tamanho de lote fixo.
- Todos os comentários e descrições de parâmetros estão em inglês para consistência com as diretrizes do repositório.

## Lista de verificação de validação
- ✅ Arquivo de estratégia compilado com as convenções de modelo de estratégia StockSharp (guias, namespace com escopo de arquivo, documento herdado XML).
- ✅ Parâmetros criados via `Param()` e marcados para otimização quando apropriado.
- ✅ Williams %R valores consumidos por meio de `Bind`, sem qualquer acesso direto a `GetValue()`.
