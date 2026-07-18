# Estratégia de Impulso de Preço
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Impulso de Preço analisa cotações Level1 brutas e reage a mudanças súbitas entre o melhor bid e o melhor ask. Ela replica o expert advisor original do MetaTrader 5, monitorando saltos de preço ao longo de um número configurável de ticks e entrando no mercado quando o movimento excede um limiar baseado em pontos. Níveis de stop loss e take profit protetores são aplicados automaticamente através do assistente de alto nível `StartProtection`.

A abordagem é neutra ao mercado: uma posição comprada é aberta quando o preço ask sobe em comparação com uma cotação mais antiga, enquanto uma posição vendida é aberta quando o bid colapsa abaixo de seu valor anterior. Um período de espera configurável impede que a estratégia entre novamente imediatamente após uma operação, assim como a implementação MQL que aguarda um intervalo de pausa especificado.

## Como Funciona

- Inscreve-se em dados Level1 e armazena históricos contínuos dos melhores preços bid e ask.
- Calcula a diferença de preço entre a cotação mais recente e a cotação que chegou `HistoryGap` ticks antes (com buffer adicional definido por `ExtraHistory`).
- Abre uma posição comprada quando o preço ask sobe mais de `ImpulsePoints * PriceStep` e não existe exposição comprada.
- Abre uma posição vendida quando o preço bid cai mais do mesmo limiar e não existe exposição vendida.
- Aplica níveis fixos de take profit e stop loss expressos em pontos de preço e impõe uma pausa de `CooldownSeconds` entre ordens.

## Parâmetros

- **OrderVolume** – volume enviado com cada ordem de mercado. Padrão é `0.1` lotes para corresponder ao robô fonte, mas pode ser otimizado para outros instrumentos.
- **StopLossPoints** – distância do preço de entrada até o stop protetor, medida em pontos do instrumento. Um valor de `0` desabilita o stop.
- **TakeProfitPoints** – distância até o objetivo de take profit, também medida em pontos. Um valor de `0` desabilita o objetivo.
- **ImpulsePoints** – impulso mínimo de preço, em pontos, que deve ser excedido entre a cotação atual e a cotação `HistoryGap` ticks atrás para acionar uma entrada.
- **HistoryGap** – número de atualizações Level1 separando o preço atual da linha de base de comparação. Valores mais altos requerem períodos de lookback maiores, o que suaviza o ruído mas atrasa as entradas.
- **ExtraHistory** – amostras Level1 adicionais retidas no buffer contínuo para absorver rajadas de cotações quando vários ticks chegam entre callbacks. Mantém a lógica consistente com a implementação MT5 que sobre-amostra o array de histórico.
- **CooldownSeconds** – tempo mínimo de espera após qualquer operação antes que outra entrada possa ser colocada. Garante que a estratégia replique o parâmetro `InpSleep` do expert MQL e previne inversões rápidas.

## Notas

- Os parâmetros de distância em pontos são convertidos automaticamente usando `Security.PriceStep` (ou `Security.MinPriceStep` como fallback), para que a mesma configuração se adapte a diferentes tamanhos de tick.
- O trading só começa quando a estratégia está online, os buffers de histórico contêm dados suficientes e a condição de impulso é satisfeita.
- Como as decisões são tomadas em atualizações de cotações brutas, a estratégia funciona melhor em instrumentos líquidos com feeds Level1 confiáveis.
- Não existe versão em Python para esta estratégia. Apenas a versão em C# é fornecida, atendendo à solicitação do usuário.
