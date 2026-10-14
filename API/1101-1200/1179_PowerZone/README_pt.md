# Estratégia PowerZone
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia identifica blocos de ordens "PowerZone" criados por uma vela bearish seguida de velas bullish consecutivas (e vice-versa). Um rompimento acima da zona bullish aciona uma operação comprada, enquanto uma quebra abaixo da zona bearish abre uma vendida. Alvos e stops são baseados no intervalo da zona.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Vela bearish há `Periods+1` barras seguida de `Periods` velas bullish e o preço rompe acima do máximo da zona.
  - **Vendido**: Vela bullish há `Periods+1` barras seguida de `Periods` velas bearish e o preço rompe abaixo do mínimo da zona.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Take profit e stop loss como múltiplos do intervalo da zona.
- **Indicadores**: Nenhum.
- **Valores padrão**:
  - `Periods` = 5
  - `Threshold` = 0
  - `UseWicks` = false
  - `Take Profit Factor` = 1.5
  - `Stop Loss Factor` = 1
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Moderado

## Cálculo e execução

C# e Python usam velas concluídas. Na barra de decisão `t`, a âncora é `t - Periods - 1`; as próximas `Periods` barras até `t - 1` precisam fechar todas estritamente acima da abertura para zona de alta, ou abaixo para zona de baixa. A âncora tem cor estritamente oposta; dojis não qualificam. A vela atual não confirma a zona, que nunca é conhecida antes de concluir a sequência.

Exija `100 * abs(Close[t - 1] - Close da âncora) / abs(Close da âncora) >= Threshold`; fechamento da âncora zero é inválido. A zona é seu corpo `[min(Open, Close), max(Open, Close)]` ou `[Low, High]` com `UseWicks=true`. Extremos da confirmação não ampliam a zona; a largura deve ser positiva. Guarde separadamente as últimas zonas de alta e baixa até substituí-las, com no máximo uma entrada aceita por zona.

Compra exige `Close atual > máxima da zona de alta`; venda, `Close < mínima da zona de baixa`. Igualdade mantém a posição. Uma zona nova pode entrar na primeira vela de decisão mesmo se o preço já estiver fora. Entre com `Volume` sem posição ou reverta completamente com `Volume + abs(Position)`, sem aumentar o mesmo lado. Se ambas as direções qualificarem sem posição, compra tem prioridade. Ordens pendentes bloqueiam outras ordens. Não há EMA, espera obrigatória ou outros indicadores.

Na entrada aceita, congele `largura * StopLossFactor` e `largura * TakeProfitFactor` ao redor da execução real. Aumentos parciais do mesmo lado usam base ponderada por volume; reduzir preserva a base e reverter a reinicia. Zonas posteriores não movem as distâncias. Proteção local verifica bid executável para compras, ask para vendas e fechamentos de velas; fecha toda a quantidade restante a mercado, antes de novas entradas na mesma vela. Toques nas máximas/mínimas não pressupõem execuções intrabar; lacunas podem alterar o preço executado.

Padrões compartilhados: parâmetros API `TakeProfitFactor=1.5`, `StopLossFactor=1`, `CandleType` de cinco minutos. O parâmetro publicado é `UseWicks`; a propriedade C# é `IsUseWicks`. `Periods` e fatores positivos; `Threshold` é percentual não negativo. Essas definições esclarecem regras de limiar, limites, uso único e base de execução antes não especificadas.
