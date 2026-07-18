# Estratégia Doji Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia replica a lógica central do clássico expert advisor **Doji Trader**.
Monitora velas concluídas em busca de padrões doji de corpo compacto e aguarda um
fechamento de rompimento além do range do doji para entrar no mercado na direção do rompimento.

## Lógica de trading

1. Apenas velas terminadas são processadas. O período padrão é 1 hora, mas pode ser
   ajustado através do parâmetro `CandleType`.
2. O trading só é permitido quando o horário de fechamento da última vela cai dentro da
   janela de sessão configurável `[StartHour, EndHour)` medida no horário do exchange.
3. O algoritmo mantém as três velas terminadas mais recentes na memória. A vela que
   acabou de fechar é comparada com as duas velas que a antecederam (`-2` e `-3`).
4. Uma vela conta como doji quando a diferença absoluta entre sua abertura e fechamento é
   menor que `MaximumDojiHeight * pip`, onde o valor do pip é derivado do passo de preço
   do instrumento (cotações de 3 ou 5 dígitos são automaticamente escaladas ×10).
5. Se a vela mais nova fecha **acima** do máximo do doji qualificante mais recente, a
   estratégia abre (ou reverte para) uma posição comprada. Se fecha **abaixo** do mínimo do doji,
   abre uma posição vendida. Nenhuma operação é colocada quando o preço permanece dentro do range do doji.
6. O tamanho da posição é retirado da propriedade `Volume` da estratégia. Quando um sinal de reversão
   aparece, o algoritmo envia volume suficiente para fechar a posição anterior e estabelecer
   a exposição desejada na nova direção, de modo que apenas uma posição líquida permaneça aberta.

## Gestão de risco

- As distâncias de stop-loss e take-profit são configuradas em pips através de `StopLossPips` e
  `TakeProfitPips`. Definir um valor como zero desabilita a ordem de proteção correspondente.
- `StartProtection` é lançado uma vez na inicialização e usa ordens a mercado para saídas, de modo que o
  comportamento espelha a implementação MQL que fechou e reabriu posições diretamente.

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| `CandleType` | Período das velas processadas. | Período de 1 hora |
| `StartHour` | Hora de abertura inclusiva da janela de trading. | 8 |
| `EndHour` | Hora de fechamento exclusiva da janela de trading. | 17 |
| `MaximumDojiHeight` | Altura máxima do corpo (em pips) para uma vela ser tratada como doji. | 1 |
| `StopLossPips` | Distância do stop de proteção em pips. | 50 |
| `TakeProfitPips` | Distância do alvo de lucro em pips. | 50 |

### Notas adicionais

- A estratégia assume que a conta da plataforma usa posições líquidas. Se seu feed fornece
  passos de pip fracionários (cotações de 5 ou 3 dígitos), o valor do pip é multiplicado por 10 para
  corresponder às medições tradicionais de pip.
- Defina o tamanho de lote desejado na propriedade `Volume` antes de executar a estratégia.
- Não são necessários indicadores adicionais; a lógica depende apenas de dados brutos de velas.
- Ainda não há port em Python; existe apenas a implementação em C#.
