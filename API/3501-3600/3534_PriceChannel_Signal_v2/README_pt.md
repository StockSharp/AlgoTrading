# Estratégia PriceChannel Signal v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
PriceChannel Signal v2 é um sistema de breakout de acompanhamento de tendências construído em torno de um canal Donchian modificado. O consultor especialista MQL5 original observa transições na tendência do canal, condições opcionais de reentrada quando o preço retrocede através das bandas e níveis de saída protetores derivados da mesma faixa. A porta StockSharp mantém o comportamento original: negocia uma única posição por vez, reage apenas a velas concluídas e pode ser restrita a uma janela intradiária.

## Lógica de negociação
1. O alto e o baixo do canal Donchian são calculados sobre o `ChannelPeriod` configurado.
2. O intervalo bruto é alterado por dois multiplicadores:
   * **Fator de Risco** – comprime as bandas de entrada em direção à mediana do canal.
   * **Nível de saída** – cria um segundo par de faixas internas que acionam saídas.
3. Um estado de tendência é mantido:
   * Quando o fechamento ultrapassa a banda de entrada superior, a tendência torna-se de alta.
   * Quando o fechamento quebra abaixo da banda de entrada inferior, a tendência torna-se de baixa.
   * Caso contrário, a tendência anterior é mantida.
4. Sinais gerados a partir desse estado:
   * **Entrada longa** – a tendência muda de baixa para alta.
   * **Entrada curta** – a tendência muda de alta para baixa.
   * **Reentrada longa** – opcional, o preço fecha acima da banda superior enquanto a tendência já é de alta.
   * **Reentrada curta** – opcional, o preço fecha abaixo da banda inferior enquanto a tendência já é de baixa.
   * **Saída longa** – opcional, o preço fecha abaixo da banda de saída de alta após estar acima dela na barra anterior.
   * **Saída curta** – opcional, o preço fecha acima da banda de saída de baixa após ficar abaixo dela na barra anterior.
5. É permitido apenas um pedido por barra e por direção. A estratégia recusa-se a abrir uma nova posição se outra já estiver ativa.
6. Se o filtro intradiário estiver habilitado, todos os sinais acima serão ignorados fora da janela configurada.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `ChannelPeriod` | Donchian comprimento de lookback usado para calcular o canal de preço e as faixas de saída. |
| `RiskFactor` | Mudança das bandas de entrada (0–10). Valores mais baixos ampliam as bandas, valores mais altos as estreitam. |
| `ExitLevel` | Mudança das bandas de saída. Deve ser maior que `RiskFactor` para permanecer dentro do intervalo de entrada. |
| `UseReEntry` | Permite negociações de reentrada quando o preço retrocede através da banda ativa. |
| `UseExitSignals` | Ativa a lógica de saída com base nas faixas de proteção internas. |
| `CandleType` | Prazo usado para construir velas e executar os indicadores. |
| `UseTimeControl` | Alterna a janela de negociação intradiária. |
| `StartHour` / `StartMinute` | Início inclusivo da janela de negociação quando o controle de tempo estiver ativo. |
| `EndHour` / `EndMinute` | Fim exclusivo da janela de negociação quando o controle de tempo estiver ativo. |

## Regras de entrada e saída
* **Enter long:** a tendência muda para alta ou condições de reentrada disparam, a posição atual é plana e a barra está dentro da janela de tempo permitida.
* **Enter short:** a tendência muda para baixa ou a condição de reentrada curta é disparada, a posição atual é plana e a barra está dentro da janela de tempo permitida.
* **Saída longa:** `UseExitSignals` está habilitado e o fechamento fica abaixo da banda de saída após estar acima dela na barra anterior.
* **Saída curta:** `UseExitSignals` está habilitado e o fechamento sobe acima da banda de saída depois de ficar abaixo dela na barra anterior.

## Notas adicionais
* A estratégia funciona com ordens de mercado e não faz pirâmide de posições.
* Os valores dos indicadores são processados apenas em velas acabadas para evitar a repintura intrabarra.
* O volume padrão é 1 contrato se não for fornecido explicitamente.
* O controle de tempo segue o comportamento original EA: o horário de término é exclusivo e há suporte para agrupamento até a meia-noite.
